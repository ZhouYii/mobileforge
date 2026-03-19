#!/usr/bin/env python3
"""
Generate Tower of Saviors game data using authentic stat curves and English names
sourced from the TOS-CARD-API and Fandom wiki.

Usage:
    python generate_data.py                     # Generate all JSON files to stdout dir
    python generate_data.py --output ../shared/data/
    python generate_data.py --validate ../shared/data/
"""

import json
import os
import sys
import argparse
import math
from pathlib import Path

# ==============================================================================
# AUTHENTIC DATA from TOS-CARD-API + Fandom Wiki
# Stats format: (base_hp, base_atk, base_rec, max_hp, max_atk, max_rec)
# ==============================================================================

# Element mapping: 1=Water, 2=Fire, 3=Earth, 4=Light, 5=Dark, 6=Heart/Neutral

# --- Existing monsters (IDs 1-20, 101-103) are preserved exactly ---
EXISTING_MONSTERS = [
    {"id":1,"name":"Water Dragon","element":1,"rarity":5,"max_level":99,
     "base_hp":800,"base_atk":350,"base_rec":150,"max_hp":3200,"max_atk":1400,"max_rec":350,
     "cost":25,"active_skill_id":1,"leader_skill_id":1,"evolve_to":11,
     "evolve_materials":[101,102],"exp_curve":"standard","awakening_slots":[1,2,4,10,6]},
    {"id":2,"name":"Fire Phoenix","element":2,"rarity":5,"max_level":99,
     "base_hp":750,"base_atk":400,"base_rec":130,"max_hp":3000,"max_atk":1600,"max_rec":300,
     "cost":25,"active_skill_id":2,"leader_skill_id":2,"evolve_to":12,
     "evolve_materials":[101,103],"exp_curve":"standard","awakening_slots":[2,2,4,10,11]},
    {"id":3,"name":"Earth Golem","element":3,"rarity":5,"max_level":99,
     "base_hp":950,"base_atk":300,"base_rec":120,"max_hp":3800,"max_atk":1200,"max_rec":280,
     "cost":25,"active_skill_id":3,"leader_skill_id":3,"evolve_to":13,
     "evolve_materials":[102,103],"exp_curve":"standard","awakening_slots":[1,1,4,12,6]},
    {"id":4,"name":"Light Angel","element":4,"rarity":5,"max_level":99,
     "base_hp":820,"base_atk":330,"base_rec":200,"max_hp":3300,"max_atk":1320,"max_rec":450,
     "cost":25,"active_skill_id":4,"leader_skill_id":4,"evolve_to":14,
     "evolve_materials":[101,102,103],"exp_curve":"standard"},
    {"id":5,"name":"Dark Reaper","element":5,"rarity":5,"max_level":99,
     "base_hp":780,"base_atk":420,"base_rec":100,"max_hp":3100,"max_atk":1680,"max_rec":250,
     "cost":25,"active_skill_id":5,"leader_skill_id":5,"evolve_to":15,
     "evolve_materials":[101,103],"exp_curve":"standard"},
    {"id":6,"name":"Aqua Knight","element":1,"rarity":4,"max_level":70,
     "base_hp":550,"base_atk":230,"base_rec":100,"max_hp":2000,"max_atk":900,"max_rec":220,
     "cost":15,"active_skill_id":6,"leader_skill_id":None,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"standard"},
    {"id":7,"name":"Flame Warrior","element":2,"rarity":4,"max_level":70,
     "base_hp":500,"base_atk":270,"base_rec":90,"max_hp":1850,"max_atk":1050,"max_rec":200,
     "cost":15,"active_skill_id":7,"leader_skill_id":None,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"standard"},
    {"id":8,"name":"Forest Guardian","element":3,"rarity":4,"max_level":70,
     "base_hp":620,"base_atk":200,"base_rec":85,"max_hp":2300,"max_atk":780,"max_rec":190,
     "cost":15,"active_skill_id":8,"leader_skill_id":None,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"standard"},
    {"id":9,"name":"Holy Paladin","element":4,"rarity":4,"max_level":70,
     "base_hp":560,"base_atk":220,"base_rec":140,"max_hp":2050,"max_atk":860,"max_rec":310,
     "cost":15,"active_skill_id":9,"leader_skill_id":None,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"standard"},
    {"id":10,"name":"Shadow Assassin","element":5,"rarity":4,"max_level":70,
     "base_hp":480,"base_atk":290,"base_rec":75,"max_hp":1800,"max_atk":1120,"max_rec":170,
     "cost":15,"active_skill_id":10,"leader_skill_id":None,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"standard"},
    {"id":11,"name":"Abyssal Sea Dragon","element":1,"rarity":6,"max_level":99,
     "base_hp":1200,"base_atk":520,"base_rec":200,"max_hp":4500,"max_atk":2000,"max_rec":480,
     "cost":35,"active_skill_id":1,"leader_skill_id":1,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced"},
    {"id":12,"name":"Inferno Phoenix","element":2,"rarity":6,"max_level":99,
     "base_hp":1100,"base_atk":600,"base_rec":180,"max_hp":4200,"max_atk":2300,"max_rec":420,
     "cost":35,"active_skill_id":2,"leader_skill_id":2,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced"},
    {"id":13,"name":"Ancient Earth Titan","element":3,"rarity":6,"max_level":99,
     "base_hp":1400,"base_atk":450,"base_rec":160,"max_hp":5200,"max_atk":1750,"max_rec":380,
     "cost":35,"active_skill_id":3,"leader_skill_id":3,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced"},
    {"id":14,"name":"Archangel of Dawn","element":4,"rarity":6,"max_level":99,
     "base_hp":1250,"base_atk":500,"base_rec":280,"max_hp":4700,"max_atk":1900,"max_rec":620,
     "cost":35,"active_skill_id":4,"leader_skill_id":4,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced"},
    {"id":15,"name":"Grim Harvester","element":5,"rarity":6,"max_level":99,
     "base_hp":1150,"base_atk":630,"base_rec":140,"max_hp":4300,"max_atk":2400,"max_rec":340,
     "cost":35,"active_skill_id":5,"leader_skill_id":5,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced"},
    {"id":16,"name":"Water Sprite","element":1,"rarity":3,"max_level":50,
     "base_hp":350,"base_atk":150,"base_rec":70,"max_hp":1100,"max_atk":500,"max_rec":150,
     "cost":8,"active_skill_id":6,"leader_skill_id":None,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"fast"},
    {"id":17,"name":"Fire Imp","element":2,"rarity":3,"max_level":50,
     "base_hp":320,"base_atk":170,"base_rec":60,"max_hp":1000,"max_atk":560,"max_rec":130,
     "cost":8,"active_skill_id":7,"leader_skill_id":None,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"fast"},
    {"id":18,"name":"Moss Turtle","element":3,"rarity":3,"max_level":50,
     "base_hp":400,"base_atk":130,"base_rec":65,"max_hp":1300,"max_atk":430,"max_rec":140,
     "cost":8,"active_skill_id":8,"leader_skill_id":None,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"fast"},
    {"id":19,"name":"Light Wisp","element":4,"rarity":3,"max_level":50,
     "base_hp":340,"base_atk":140,"base_rec":90,"max_hp":1050,"max_atk":470,"max_rec":200,
     "cost":8,"active_skill_id":9,"leader_skill_id":None,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"fast"},
    {"id":20,"name":"Dark Bat","element":5,"rarity":3,"max_level":50,
     "base_hp":300,"base_atk":180,"base_rec":55,"max_hp":950,"max_atk":600,"max_rec":120,
     "cost":8,"active_skill_id":10,"leader_skill_id":None,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"fast"},
    {"id":101,"name":"Elemental Shard","element":6,"rarity":1,"max_level":1,
     "base_hp":50,"base_atk":10,"base_rec":10,"max_hp":50,"max_atk":10,"max_rec":10,
     "cost":1,"active_skill_id":None,"leader_skill_id":None,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"flat"},
    {"id":102,"name":"Dragon Scale","element":6,"rarity":2,"max_level":1,
     "base_hp":80,"base_atk":20,"base_rec":15,"max_hp":80,"max_atk":20,"max_rec":15,
     "cost":2,"active_skill_id":None,"leader_skill_id":None,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"flat"},
    {"id":103,"name":"Phoenix Feather","element":6,"rarity":2,"max_level":1,
     "base_hp":70,"base_atk":25,"base_rec":20,"max_hp":70,"max_atk":25,"max_rec":20,
     "cost":2,"active_skill_id":None,"leader_skill_id":None,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"flat"},
]

# New monsters with authentic ToS data
# Greek Gods (IDs 21-25 base 5★, 51-55 evolved 6★)
# Real stats from TOS-CARD-API
GREEK_GODS_BASE = [
    # Poseidon - Water, tanky
    {"id":21,"name":"Poseidon","element":1,"rarity":5,"max_level":99,
     "base_hp":676,"base_atk":427,"base_rec":92,"max_hp":1315,"max_atk":777,"max_rec":205,
     "cost":15,"active_skill_id":20,"leader_skill_id":6,"evolve_to":51,
     "evolve_materials":[104,104,108],"exp_curve":"standard","series_id":"greek_gods","group":"God"},
    # Hephaestus - Fire, ATK-heavy
    {"id":22,"name":"Hephaestus","element":2,"rarity":5,"max_level":99,
     "base_hp":712,"base_atk":458,"base_rec":81,"max_hp":1384,"max_atk":831,"max_rec":181,
     "cost":15,"active_skill_id":21,"leader_skill_id":7,"evolve_to":52,
     "evolve_materials":[105,105,108],"exp_curve":"standard","series_id":"greek_gods","group":"God"},
    # Athena - Earth, HP-heavy
    {"id":23,"name":"Athena","element":3,"rarity":5,"max_level":99,
     "base_hp":783,"base_atk":393,"base_rec":86,"max_hp":1521,"max_atk":715,"max_rec":191,
     "cost":15,"active_skill_id":22,"leader_skill_id":8,"evolve_to":53,
     "evolve_materials":[106,106,108],"exp_curve":"standard","series_id":"greek_gods","group":"God"},
    # Apollo - Light, REC-heavy
    {"id":24,"name":"Apollo","element":4,"rarity":5,"max_level":99,
     "base_hp":705,"base_atk":398,"base_rec":94,"max_hp":1370,"max_atk":723,"max_rec":210,
     "cost":15,"active_skill_id":23,"leader_skill_id":9,"evolve_to":54,
     "evolve_materials":[107,107,108],"exp_curve":"standard","series_id":"greek_gods","group":"God"},
    # Artemis - Dark, glass cannon
    {"id":25,"name":"Artemis","element":5,"rarity":5,"max_level":99,
     "base_hp":662,"base_atk":471,"base_rec":85,"max_hp":1287,"max_atk":854,"max_rec":189,
     "cost":15,"active_skill_id":24,"leader_skill_id":10,"evolve_to":55,
     "evolve_materials":[104,107,108],"exp_curve":"standard","series_id":"greek_gods","group":"God"},
]
GREEK_GODS_EVOLVED = [
    {"id":51,"name":"Poseidon, God of the Sea","element":1,"rarity":6,"max_level":99,
     "base_hp":1290,"base_atk":816,"base_rec":175,"max_hp":2555,"max_atk":1508,"max_rec":398,
     "cost":30,"active_skill_id":20,"leader_skill_id":6,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced","series_id":"greek_gods","group":"God"},
    {"id":52,"name":"Hephaestus, God of Fire","element":2,"rarity":6,"max_level":99,
     "base_hp":1358,"base_atk":872,"base_rec":155,"max_hp":2689,"max_atk":1612,"max_rec":353,
     "cost":30,"active_skill_id":21,"leader_skill_id":7,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced","series_id":"greek_gods","group":"God"},
    {"id":53,"name":"Athena, Goddess of Wisdom","element":3,"rarity":6,"max_level":99,
     "base_hp":1493,"base_atk":751,"base_rec":164,"max_hp":2957,"max_atk":1389,"max_rec":373,
     "cost":30,"active_skill_id":22,"leader_skill_id":8,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced","series_id":"greek_gods","group":"God"},
    {"id":54,"name":"Apollo, God of the Sun","element":4,"rarity":6,"max_level":99,
     "base_hp":1344,"base_atk":759,"base_rec":180,"max_hp":2662,"max_atk":1403,"max_rec":409,
     "cost":30,"active_skill_id":23,"leader_skill_id":9,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced","series_id":"greek_gods","group":"God"},
    {"id":55,"name":"Artemis, Goddess of the Moon","element":5,"rarity":6,"max_level":99,
     "base_hp":1264,"base_atk":896,"base_rec":162,"max_hp":2503,"max_atk":1657,"max_rec":369,
     "cost":30,"active_skill_id":24,"leader_skill_id":10,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced","series_id":"greek_gods","group":"God"},
]

# Norse Gods (IDs 26-30 base, 56-60 evolved)
NORSE_GODS_BASE = [
    {"id":26,"name":"Freyr","element":1,"rarity":5,"max_level":99,
     "base_hp":837,"base_atk":397,"base_rec":102,"max_hp":1626,"max_atk":722,"max_rec":227,
     "cost":15,"active_skill_id":25,"leader_skill_id":11,"evolve_to":56,
     "evolve_materials":[104,104,109],"exp_curve":"standard","series_id":"norse_gods","group":"God"},
    {"id":27,"name":"Tyr","element":2,"rarity":5,"max_level":99,
     "base_hp":881,"base_atk":425,"base_rec":90,"max_hp":1711,"max_atk":773,"max_rec":201,
     "cost":15,"active_skill_id":26,"leader_skill_id":12,"evolve_to":57,
     "evolve_materials":[105,105,109],"exp_curve":"standard","series_id":"norse_gods","group":"God"},
    {"id":28,"name":"Freyja","element":3,"rarity":5,"max_level":99,
     "base_hp":968,"base_atk":366,"base_rec":95,"max_hp":1881,"max_atk":665,"max_rec":212,
     "cost":15,"active_skill_id":27,"leader_skill_id":13,"evolve_to":58,
     "evolve_materials":[106,106,109],"exp_curve":"standard","series_id":"norse_gods","group":"God"},
    {"id":29,"name":"Thor","element":4,"rarity":5,"max_level":99,
     "base_hp":872,"base_atk":370,"base_rec":104,"max_hp":1694,"max_atk":673,"max_rec":232,
     "cost":15,"active_skill_id":28,"leader_skill_id":14,"evolve_to":59,
     "evolve_materials":[107,107,109],"exp_curve":"standard","series_id":"norse_gods","group":"God"},
    {"id":30,"name":"Loki","element":5,"rarity":5,"max_level":99,
     "base_hp":820,"base_atk":438,"base_rec":94,"max_hp":1593,"max_atk":794,"max_rec":210,
     "cost":15,"active_skill_id":29,"leader_skill_id":15,"evolve_to":60,
     "evolve_materials":[104,107,109],"exp_curve":"standard","series_id":"norse_gods","group":"God"},
]
NORSE_GODS_EVOLVED = [
    {"id":56,"name":"Freyr, Victorious Deity of the Sea","element":1,"rarity":6,"max_level":99,
     "base_hp":1595,"base_atk":758,"base_rec":195,"max_hp":3160,"max_atk":1401,"max_rec":443,
     "cost":30,"active_skill_id":25,"leader_skill_id":11,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced","series_id":"norse_gods","group":"God"},
    {"id":57,"name":"Tyr, One-armed Deity of War","element":2,"rarity":6,"max_level":99,
     "base_hp":1679,"base_atk":811,"base_rec":172,"max_hp":3326,"max_atk":1499,"max_rec":392,
     "cost":30,"active_skill_id":26,"leader_skill_id":12,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced","series_id":"norse_gods","group":"God"},
    {"id":58,"name":"Freyja, Fair Deity of Love","element":3,"rarity":6,"max_level":99,
     "base_hp":1845,"base_atk":698,"base_rec":181,"max_hp":3655,"max_atk":1291,"max_rec":412,
     "cost":30,"active_skill_id":27,"leader_skill_id":13,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced","series_id":"norse_gods","group":"God"},
    {"id":59,"name":"Thor, God of Thunder","element":4,"rarity":6,"max_level":99,
     "base_hp":1662,"base_atk":706,"base_rec":198,"max_hp":3293,"max_atk":1305,"max_rec":452,
     "cost":30,"active_skill_id":28,"leader_skill_id":14,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced","series_id":"norse_gods","group":"God"},
    {"id":60,"name":"Loki, God of Tricksters","element":5,"rarity":6,"max_level":99,
     "base_hp":1563,"base_atk":833,"base_rec":180,"max_hp":3096,"max_atk":1540,"max_rec":409,
     "cost":30,"active_skill_id":29,"leader_skill_id":15,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced","series_id":"norse_gods","group":"God"},
]

# Egyptian Gods (IDs 31-35 base, 61-65 evolved) - notably low REC
EGYPTIAN_GODS_BASE = [
    {"id":31,"name":"Tefnut","element":1,"rarity":5,"max_level":99,
     "base_hp":814,"base_atk":358,"base_rec":36,"max_hp":1531,"max_atk":627,"max_rec":80,
     "cost":15,"active_skill_id":30,"leader_skill_id":16,"evolve_to":61,
     "evolve_materials":[104,104,110],"exp_curve":"standard","series_id":"egyptian_gods","group":"God"},
    {"id":32,"name":"Seth","element":2,"rarity":5,"max_level":99,
     "base_hp":942,"base_atk":363,"base_rec":13,"max_hp":1847,"max_atk":591,"max_rec":15,
     "cost":15,"active_skill_id":31,"leader_skill_id":17,"evolve_to":62,
     "evolve_materials":[105,105,110],"exp_curve":"standard","series_id":"egyptian_gods","group":"God"},
    {"id":33,"name":"Shu","element":3,"rarity":5,"max_level":99,
     "base_hp":986,"base_atk":298,"base_rec":13,"max_hp":1933,"max_atk":486,"max_rec":15,
     "cost":15,"active_skill_id":32,"leader_skill_id":18,"evolve_to":63,
     "evolve_materials":[106,106,110],"exp_curve":"standard","series_id":"egyptian_gods","group":"God"},
    {"id":34,"name":"Ra","element":4,"rarity":5,"max_level":99,
     "base_hp":848,"base_atk":333,"base_rec":37,"max_hp":1594,"max_atk":584,"max_rec":82,
     "cost":15,"active_skill_id":33,"leader_skill_id":19,"evolve_to":64,
     "evolve_materials":[107,107,110],"exp_curve":"standard","series_id":"egyptian_gods","group":"God"},
    {"id":35,"name":"Osiris","element":5,"rarity":5,"max_level":99,
     "base_hp":671,"base_atk":410,"base_rec":79,"max_hp":1305,"max_atk":746,"max_rec":177,
     "cost":15,"active_skill_id":34,"leader_skill_id":20,"evolve_to":65,
     "evolve_materials":[104,107,110],"exp_curve":"standard","series_id":"egyptian_gods","group":"God"},
]
EGYPTIAN_GODS_EVOLVED = [
    {"id":61,"name":"Tefnut, Goddess of Rain","element":1,"rarity":6,"max_level":99,
     "base_hp":1518,"base_atk":670,"base_rec":67,"max_hp":2977,"max_atk":1219,"max_rec":155,
     "cost":30,"active_skill_id":30,"leader_skill_id":16,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced","series_id":"egyptian_gods","group":"God"},
    {"id":62,"name":"Seth, Deity of Warfare","element":2,"rarity":6,"max_level":99,
     "base_hp":1796,"base_atk":692,"base_rec":25,"max_hp":3521,"max_atk":1125,"max_rec":28,
     "cost":30,"active_skill_id":31,"leader_skill_id":17,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced","series_id":"egyptian_gods","group":"God"},
    {"id":63,"name":"Shu, God of the Sky","element":3,"rarity":6,"max_level":99,
     "base_hp":1865,"base_atk":565,"base_rec":25,"max_hp":3758,"max_atk":939,"max_rec":28,
     "cost":30,"active_skill_id":32,"leader_skill_id":18,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced","series_id":"egyptian_gods","group":"God"},
    {"id":64,"name":"Ra, Child of the Sun","element":4,"rarity":6,"max_level":99,
     "base_hp":1581,"base_atk":623,"base_rec":68,"max_hp":3101,"max_atk":1134,"max_rec":159,
     "cost":30,"active_skill_id":33,"leader_skill_id":19,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced","series_id":"egyptian_gods","group":"God"},
    {"id":65,"name":"Osiris, God of the Afterlife","element":5,"rarity":6,"max_level":99,
     "base_hp":1283,"base_atk":780,"base_rec":151,"max_hp":2541,"max_atk":1442,"max_rec":344,
     "cost":30,"active_skill_id":34,"leader_skill_id":20,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced","series_id":"egyptian_gods","group":"God"},
]

# Chinese Gods (IDs 36-40 base, 66-70 evolved)
CHINESE_GODS_BASE = [
    {"id":36,"name":"Dragon of the East Sea","element":1,"rarity":5,"max_level":99,
     "base_hp":596,"base_atk":226,"base_rec":10,"max_hp":1170,"max_atk":369,"max_rec":11,
     "cost":12,"active_skill_id":35,"leader_skill_id":21,"evolve_to":66,
     "evolve_materials":[104,104,111],"exp_curve":"standard","series_id":"chinese_gods","group":"God"},
    {"id":37,"name":"Little Lotus","element":2,"rarity":5,"max_level":99,
     "base_hp":299,"base_atk":160,"base_rec":115,"max_hp":561,"max_atk":300,"max_rec":255,
     "cost":12,"active_skill_id":36,"leader_skill_id":22,"evolve_to":67,
     "evolve_materials":[105,105,111],"exp_curve":"standard","series_id":"chinese_gods","group":"God"},
    {"id":38,"name":"Bull King","element":3,"rarity":5,"max_level":99,
     "base_hp":573,"base_atk":201,"base_rec":20,"max_hp":1077,"max_atk":352,"max_rec":46,
     "cost":12,"active_skill_id":37,"leader_skill_id":23,"evolve_to":68,
     "evolve_materials":[106,106,111],"exp_curve":"standard","series_id":"chinese_gods","group":"God"},
    {"id":39,"name":"Monkey King","element":4,"rarity":5,"max_level":99,
     "base_hp":296,"base_atk":139,"base_rec":133,"max_hp":555,"max_atk":261,"max_rec":295,
     "cost":12,"active_skill_id":38,"leader_skill_id":24,"evolve_to":69,
     "evolve_materials":[107,107,111],"exp_curve":"standard","series_id":"chinese_gods","group":"God"},
    {"id":40,"name":"Nine-tailed Vixen","element":5,"rarity":5,"max_level":99,
     "base_hp":278,"base_atk":164,"base_rec":120,"max_hp":521,"max_atk":308,"max_rec":266,
     "cost":12,"active_skill_id":39,"leader_skill_id":25,"evolve_to":70,
     "evolve_materials":[104,107,111],"exp_curve":"standard","series_id":"chinese_gods","group":"God"},
]
CHINESE_GODS_EVOLVED = [
    {"id":66,"name":"Ao Guang, Dragon King of the Sea","element":1,"rarity":6,"max_level":99,
     "base_hp":1790,"base_atk":682,"base_rec":30,"max_hp":3608,"max_atk":1133,"max_rec":34,
     "cost":30,"active_skill_id":35,"leader_skill_id":21,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced","series_id":"chinese_gods","group":"God"},
    {"id":67,"name":"Nezha the Lotus Prince","element":2,"rarity":6,"max_level":99,
     "base_hp":1255,"base_atk":701,"base_rec":177,"max_hp":2464,"max_atk":1377,"max_rec":397,
     "cost":30,"active_skill_id":36,"leader_skill_id":22,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced","series_id":"chinese_gods","group":"God"},
    {"id":68,"name":"Bull King the Great Sage","element":3,"rarity":6,"max_level":99,
     "base_hp":1718,"base_atk":604,"base_rec":60,"max_hp":3370,"max_atk":997,"max_rec":69,
     "cost":30,"active_skill_id":37,"leader_skill_id":23,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced","series_id":"chinese_gods","group":"God"},
    {"id":69,"name":"Sun Wukong the Great Sage","element":4,"rarity":6,"max_level":99,
     "base_hp":1362,"base_atk":663,"base_rec":168,"max_hp":2698,"max_atk":1226,"max_rec":382,
     "cost":30,"active_skill_id":38,"leader_skill_id":24,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced","series_id":"chinese_gods","group":"God"},
    {"id":70,"name":"Daji the Demonic Enchantress","element":5,"rarity":6,"max_level":99,
     "base_hp":846,"base_atk":499,"base_rec":305,"max_hp":1615,"max_atk":953,"max_rec":689,
     "cost":30,"active_skill_id":39,"leader_skill_id":25,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced","series_id":"chinese_gods","group":"God"},
]

# Chinese Beasts (IDs 41-45 base, 71-75 evolved) - based on real ToS IDs 21-40
CHINESE_BEASTS_BASE = [
    {"id":41,"name":"Qinglong the Sage","element":1,"rarity":5,"max_level":99,
     "base_hp":780,"base_atk":410,"base_rec":88,"max_hp":1520,"max_atk":750,"max_rec":196,
     "cost":15,"active_skill_id":40,"leader_skill_id":26,"evolve_to":71,
     "evolve_materials":[104,108,109],"exp_curve":"standard","series_id":"chinese_beasts","group":"Beast"},
    {"id":42,"name":"Zhuque the Sage","element":2,"rarity":5,"max_level":99,
     "base_hp":820,"base_atk":440,"base_rec":78,"max_hp":1600,"max_atk":800,"max_rec":174,
     "cost":15,"active_skill_id":41,"leader_skill_id":27,"evolve_to":72,
     "evolve_materials":[105,108,109],"exp_curve":"standard","series_id":"chinese_beasts","group":"Beast"},
    {"id":43,"name":"Xuanwu the Sage","element":3,"rarity":5,"max_level":99,
     "base_hp":900,"base_atk":360,"base_rec":82,"max_hp":1750,"max_atk":655,"max_rec":183,
     "cost":15,"active_skill_id":42,"leader_skill_id":28,"evolve_to":73,
     "evolve_materials":[106,108,109],"exp_curve":"standard","series_id":"chinese_beasts","group":"Beast"},
    {"id":44,"name":"Baihu the Sage","element":4,"rarity":5,"max_level":99,
     "base_hp":760,"base_atk":390,"base_rec":98,"max_hp":1480,"max_atk":710,"max_rec":218,
     "cost":15,"active_skill_id":43,"leader_skill_id":29,"evolve_to":74,
     "evolve_materials":[107,108,109],"exp_curve":"standard","series_id":"chinese_beasts","group":"Beast"},
    {"id":45,"name":"Taotie the Sage","element":5,"rarity":5,"max_level":99,
     "base_hp":740,"base_atk":460,"base_rec":75,"max_hp":1440,"max_atk":835,"max_rec":168,
     "cost":15,"active_skill_id":44,"leader_skill_id":30,"evolve_to":75,
     "evolve_materials":[104,107,109],"exp_curve":"standard","series_id":"chinese_beasts","group":"Beast"},
]
CHINESE_BEASTS_EVOLVED = [
    {"id":71,"name":"Qinglong, Azure Dragon of the East","element":1,"rarity":6,"max_level":99,
     "base_hp":1488,"base_atk":782,"base_rec":168,"max_hp":2948,"max_atk":1446,"max_rec":382,
     "cost":30,"active_skill_id":40,"leader_skill_id":26,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced","series_id":"chinese_beasts","group":"Beast"},
    {"id":72,"name":"Zhuque, Vermilion Bird of the South","element":2,"rarity":6,"max_level":99,
     "base_hp":1564,"base_atk":839,"base_rec":149,"max_hp":3098,"max_atk":1550,"max_rec":339,
     "cost":30,"active_skill_id":41,"leader_skill_id":27,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced","series_id":"chinese_beasts","group":"Beast"},
    {"id":73,"name":"Xuanwu, Black Tortoise of the North","element":3,"rarity":6,"max_level":99,
     "base_hp":1716,"base_atk":686,"base_rec":157,"max_hp":3400,"max_atk":1268,"max_rec":357,
     "cost":30,"active_skill_id":42,"leader_skill_id":28,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced","series_id":"chinese_beasts","group":"Beast"},
    {"id":74,"name":"Baihu, White Tiger of the West","element":4,"rarity":6,"max_level":99,
     "base_hp":1450,"base_atk":744,"base_rec":187,"max_hp":2873,"max_atk":1375,"max_rec":426,
     "cost":30,"active_skill_id":43,"leader_skill_id":29,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced","series_id":"chinese_beasts","group":"Beast"},
    {"id":75,"name":"Taotie, Gluttonous Beast of the Center","element":5,"rarity":6,"max_level":99,
     "base_hp":1411,"base_atk":877,"base_rec":143,"max_hp":2795,"max_atk":1621,"max_rec":326,
     "cost":30,"active_skill_id":44,"leader_skill_id":30,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced","series_id":"chinese_beasts","group":"Beast"},
]

# Evolution Materials (IDs 104-115) - one per element + multi-element
EVOLUTION_MATERIALS = [
    {"id":104,"name":"Water Evo Soul","element":1,"rarity":2,"max_level":1,
     "base_hp":60,"base_atk":15,"base_rec":12,"max_hp":60,"max_atk":15,"max_rec":12,
     "cost":1,"active_skill_id":None,"leader_skill_id":None,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"flat"},
    {"id":105,"name":"Fire Evo Soul","element":2,"rarity":2,"max_level":1,
     "base_hp":55,"base_atk":18,"base_rec":10,"max_hp":55,"max_atk":18,"max_rec":10,
     "cost":1,"active_skill_id":None,"leader_skill_id":None,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"flat"},
    {"id":106,"name":"Earth Evo Soul","element":3,"rarity":2,"max_level":1,
     "base_hp":70,"base_atk":12,"base_rec":11,"max_hp":70,"max_atk":12,"max_rec":11,
     "cost":1,"active_skill_id":None,"leader_skill_id":None,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"flat"},
    {"id":107,"name":"Light Evo Soul","element":4,"rarity":2,"max_level":1,
     "base_hp":58,"base_atk":14,"base_rec":16,"max_hp":58,"max_atk":14,"max_rec":16,
     "cost":1,"active_skill_id":None,"leader_skill_id":None,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"flat"},
    {"id":108,"name":"Dark Evo Soul","element":5,"rarity":2,"max_level":1,
     "base_hp":52,"base_atk":20,"base_rec":8,"max_hp":52,"max_atk":20,"max_rec":8,
     "cost":1,"active_skill_id":None,"leader_skill_id":None,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"flat"},
    {"id":109,"name":"Spirit Jewel","element":6,"rarity":3,"max_level":1,
     "base_hp":100,"base_atk":30,"base_rec":25,"max_hp":100,"max_atk":30,"max_rec":25,
     "cost":3,"active_skill_id":None,"leader_skill_id":None,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"flat"},
    {"id":110,"name":"Ancient Tablet","element":6,"rarity":3,"max_level":1,
     "base_hp":90,"base_atk":35,"base_rec":20,"max_hp":90,"max_atk":35,"max_rec":20,
     "cost":3,"active_skill_id":None,"leader_skill_id":None,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"flat"},
    {"id":111,"name":"Celestial Jade","element":6,"rarity":3,"max_level":1,
     "base_hp":95,"base_atk":28,"base_rec":30,"max_hp":95,"max_atk":28,"max_rec":30,
     "cost":3,"active_skill_id":None,"leader_skill_id":None,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"flat"},
    {"id":112,"name":"Runestone Fragment","element":6,"rarity":1,"max_level":1,
     "base_hp":40,"base_atk":8,"base_rec":8,"max_hp":40,"max_atk":8,"max_rec":8,
     "cost":1,"active_skill_id":None,"leader_skill_id":None,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"flat"},
    {"id":113,"name":"Mystic Shard","element":6,"rarity":2,"max_level":1,
     "base_hp":65,"base_atk":22,"base_rec":18,"max_hp":65,"max_atk":22,"max_rec":18,
     "cost":2,"active_skill_id":None,"leader_skill_id":None,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"flat"},
    {"id":114,"name":"Golden Fruit","element":6,"rarity":2,"max_level":1,
     "base_hp":75,"base_atk":15,"base_rec":25,"max_hp":75,"max_atk":15,"max_rec":25,
     "cost":2,"active_skill_id":None,"leader_skill_id":None,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"flat"},
    {"id":115,"name":"Dragon Blood Crystal","element":6,"rarity":3,"max_level":1,
     "base_hp":110,"base_atk":40,"base_rec":22,"max_hp":110,"max_atk":40,"max_rec":22,
     "cost":3,"active_skill_id":None,"leader_skill_id":None,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"flat"},
]

# Farmable Fodder (IDs 201-225) - based on real ToS slimes, gnomes, wolves, elves, witches
# 5 per element, 2-3★ with real ToS stat curves
FARMABLE_FODDER = []
FODDER_TEMPLATES = [
    # (name_template, base rarity, stat_scale)
    # Water series
    (201, "Water Slime",      1, 1, 42, 22, 26, 137, 72, 86,   1, None),
    (202, "Aqueous Slime",    1, 2, 167, 87, 99, 422, 222, 261, 3, None),
    (203, "Azure Wolf",       1, 3, 350, 165, 55, 1050, 525, 140, 6, 45),
    (204, "Sea Elf",          1, 3, 380, 150, 70, 1150, 480, 175, 7, 46),
    (205, "Frost Gnome",      1, 2, 200, 95, 45, 530, 260, 125, 4, None),
    # Fire series
    (206, "Fire Slime",       2, 1, 45, 23, 22, 145, 77, 76,   1, None),
    (207, "Flaming Slime",    2, 2, 176, 93, 88, 444, 237, 232, 3, None),
    (208, "Crimson Wolf",     2, 3, 330, 185, 48, 990, 580, 120, 6, 47),
    (209, "Fire Elf",         2, 3, 360, 170, 60, 1080, 540, 155, 7, 48),
    (210, "Lava Gnome",       2, 2, 190, 105, 38, 510, 285, 105, 4, None),
    # Earth series
    (211, "Greenwood Slime",  3, 1, 49, 20, 23, 159, 66, 80,   1, None),
    (212, "Forest Slime",     3, 2, 193, 80, 92, 488, 204, 244, 3, None),
    (213, "Emerald Wolf",     3, 3, 400, 145, 52, 1200, 460, 135, 6, 49),
    (214, "Flower Elf",       3, 3, 420, 135, 68, 1260, 430, 170, 7, 50),
    (215, "Boulder Gnome",    3, 2, 220, 85, 42, 580, 230, 115, 4, None),
    # Light series
    (216, "Light Slime",      4, 1, 44, 20, 26, 143, 67, 87,   1, None),
    (217, "Angel Slime",      4, 2, 174, 81, 102, 439, 206, 268, 3, None),
    (218, "Ivory Wolf",       4, 3, 340, 155, 65, 1020, 490, 165, 6, 51),
    (219, "Lunar Elf",        4, 3, 370, 145, 80, 1120, 465, 200, 7, 52),
    (220, "Glare Gnome",      4, 2, 195, 90, 50, 520, 245, 135, 4, None),
    # Dark series
    (221, "Dark Slime",       5, 1, 40, 24, 22, 130, 80, 74,   1, None),
    (222, "Ghost Slime",      5, 2, 160, 96, 86, 405, 245, 228, 3, None),
    (223, "Shadow Wolf",      5, 3, 310, 195, 42, 930, 615, 110, 6, 53),
    (224, "Night Elf",        5, 3, 345, 180, 55, 1035, 570, 145, 7, 54),
    (225, "Abyss Gnome",      5, 2, 185, 110, 35, 495, 300, 95,  4, None),
]
for fid, fname, felem, frar, bhp, batk, brec, mhp, matk, mrec, fcost, fskill in FODDER_TEMPLATES:
    ml = {1: 15, 2: 30, 3: 50}[frar]
    FARMABLE_FODDER.append({
        "id":fid,"name":fname,"element":felem,"rarity":frar,"max_level":ml,
        "base_hp":bhp,"base_atk":batk,"base_rec":brec,"max_hp":mhp,"max_atk":matk,"max_rec":mrec,
        "cost":fcost,"active_skill_id":fskill,"leader_skill_id":None,
        "evolve_to":None,"evolve_materials":[],"exp_curve":"fast"
    })

# Special/Collab (IDs 301-305) - 7★ powerhouses
SPECIAL_MONSTERS = [
    {"id":301,"name":"Poseidon, God of Raging Oceans","element":1,"rarity":7,"max_level":99,
     "base_hp":2100,"base_atk":1300,"base_rec":350,"max_hp":5200,"max_atk":2800,"max_rec":650,
     "cost":50,"active_skill_id":55,"leader_skill_id":6,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced","series_id":"primal_gods","group":"God"},
    {"id":302,"name":"Tyr of Incinerating Conflagration","element":2,"rarity":7,"max_level":99,
     "base_hp":1950,"base_atk":1450,"base_rec":300,"max_hp":4800,"max_atk":3100,"max_rec":560,
     "cost":50,"active_skill_id":56,"leader_skill_id":12,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced","series_id":"primal_gods","group":"God"},
    {"id":303,"name":"Athena, Goddess of Warfare","element":3,"rarity":7,"max_level":99,
     "base_hp":2300,"base_atk":1200,"base_rec":320,"max_hp":5600,"max_atk":2600,"max_rec":600,
     "cost":50,"active_skill_id":57,"leader_skill_id":8,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced","series_id":"primal_gods","group":"God"},
    {"id":304,"name":"Thor of Lustrous Fulmination","element":4,"rarity":7,"max_level":99,
     "base_hp":2000,"base_atk":1350,"base_rec":380,"max_hp":5000,"max_atk":2900,"max_rec":700,
     "cost":50,"active_skill_id":58,"leader_skill_id":14,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced","series_id":"primal_gods","group":"God"},
    {"id":305,"name":"Loki, Corruption of the Deceased","element":5,"rarity":7,"max_level":99,
     "base_hp":1800,"base_atk":1500,"base_rec":280,"max_hp":4500,"max_atk":3300,"max_rec":520,
     "cost":50,"active_skill_id":59,"leader_skill_id":15,"evolve_to":None,
     "evolve_materials":[],"exp_curve":"advanced","series_id":"primal_gods","group":"God"},
]

# ==============================================================================
# SKILLS - covers all 37 registered types from RegisterAll.cs
# ==============================================================================

def generate_active_skills():
    """60 active skills using all registered conditions, effects, and outcomes."""
    skills = [
        # --- Existing skills (IDs 1-19) preserved exactly ---
        {"id":1,"name":"Tidal Wave","description":"Deal water damage to all enemies",
         "type":"active","max_cd":10,"min_cd":5,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"area_damage","params":{"element":1,"multiplier":10.0}}]}]},
        {"id":2,"name":"Meteor Strike","description":"Deal massive fire damage to a single enemy",
         "type":"active","max_cd":12,"min_cd":6,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"single_target_damage","params":{"element":2,"multiplier":25.0,"target":"highest_hp"}}]}]},
        {"id":3,"name":"Nature's Gift","description":"Convert fire gems to earth gems",
         "type":"active","max_cd":8,"min_cd":4,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"gem_conversion","params":{"from_element":2,"to_element":3}}]}]},
        {"id":4,"name":"Divine Healing","description":"Restore HP equal to 50x recovery for 3 turns",
         "type":"active","max_cd":10,"min_cd":5,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"heal_over_time","params":{"recovery_multiplier":50.0,"duration_turns":3}}]}]},
        {"id":5,"name":"Shadow Pact","description":"Boost dark ATK by x2.0 for 2 turns, reduce HP by 25%",
         "type":"active","max_cd":14,"min_cd":8,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"atk_buff","params":{"element":5,"multiplier":2.0,"duration_turns":2}},
                               {"type":"self_damage","params":{"hp_percent":0.25}}]}]},
        {"id":6,"name":"Aqua Shield","description":"Reduce damage taken by 50% for 2 turns",
         "type":"active","max_cd":12,"min_cd":7,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"defense_buff","params":{"damage_reduction":0.5,"duration_turns":2}}]}]},
        {"id":7,"name":"Flame Delay","description":"Delay all enemies by 2 turns",
         "type":"active","max_cd":15,"min_cd":10,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"delay_enemies","params":{"turns":2,"target":"all"}}]}]},
        {"id":8,"name":"Combo Mastery","description":"For each combo above 5, increase ATK multiplier by x0.5 this turn",
         "type":"active","max_cd":8,"min_cd":4,"max_level":10,
         "rules":[{"conditions":[{"type":"combo_gte","params":{"min_combo":5}}],
                   "outcomes":[{"type":"combo_scaling_atk","params":{"bonus_per_combo":0.5,"duration_turns":1}}]},
                  {"conditions":[{"type":"combo_lt","params":{"max_combo":5}}],
                   "outcomes":[{"type":"atk_buff","params":{"element":0,"multiplier":1.2,"duration_turns":1}}]}]},
        {"id":9,"name":"Purifying Light","description":"Change own element to light for 3 turns and boost REC by x2.0",
         "type":"active","max_cd":10,"min_cd":6,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_change","params":{"target":"self","to_element":4,"duration_turns":3}},
                               {"type":"rec_buff","params":{"multiplier":2.0,"duration_turns":3}}]}]},
        {"id":10,"name":"Life Drain","description":"Deal dark damage to all enemies equal to 15x ATK, recover 30% of damage dealt",
         "type":"active","max_cd":11,"min_cd":6,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"area_damage","params":{"element":5,"multiplier":15.0}},
                               {"type":"lifesteal","params":{"percent_of_damage":0.3}}]}]},
        {"id":11,"name":"Binding Chains","description":"Bind all enemies for 2 turns (they cannot attack)",
         "type":"active","max_cd":15,"min_cd":10,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"bind","params":{"turns":2,"target":"all"}}]}]},
        {"id":12,"name":"Venom Strike","description":"Poison all enemies for 5 turns (1x ATK per turn)",
         "type":"active","max_cd":12,"min_cd":7,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"poison_dot","params":{"multiplier":1.0,"duration_turns":5}}]}]},
        {"id":13,"name":"Concussive Blow","description":"Stun all enemies (skip their next attack)",
         "type":"active","max_cd":18,"min_cd":12,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"stun","params":{"target":"all"}}]}]},
        {"id":14,"name":"Gravity Crush","description":"Deal damage equal to 30% of all enemies' max HP",
         "type":"active","max_cd":20,"min_cd":15,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"gravity_damage","params":{"percent":0.3,"target":"all"}}]}]},
        {"id":15,"name":"Retribution Shield","description":"Reflect 50% of received damage back at enemies for 3 turns",
         "type":"active","max_cd":15,"min_cd":10,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"counter_attack","params":{"reflect_percent":0.5,"duration_turns":3}}]}]},
        {"id":16,"name":"Absorb Breaker","description":"Negate enemy damage absorption for 1 turn",
         "type":"active","max_cd":20,"min_cd":15,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"void_damage_absorb","params":{"duration_turns":1}}]}]},
        {"id":17,"name":"Element Piercer","description":"Bypass enemy element shields for 1 turn",
         "type":"active","max_cd":20,"min_cd":15,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"void_element_shield","params":{"duration_turns":1}}]}]},
        {"id":18,"name":"Tidal Surge","description":"Spawn 5 water orbs on the board",
         "type":"active","max_cd":8,"min_cd":4,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"orb_spawn","params":{"element":1,"count":5}}]}]},
        {"id":19,"name":"Heart Maker","description":"Spawn 4 heart orbs on the board",
         "type":"active","max_cd":10,"min_cd":6,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"orb_spawn","params":{"element":6,"count":4}}]}]},

        # --- Greek Gods (20-24): themed AoE + utility ---
        {"id":20,"name":"Poseidon's Trident","description":"Deal 12x water damage to all enemies and reduce their defense by 30% for 2 turns",
         "type":"active","max_cd":12,"min_cd":7,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"area_damage","params":{"element":1,"multiplier":12.0}},
                               {"type":"reduce_enemy_defense","params":{"percent":0.3,"duration_turns":2}}]}]},
        {"id":21,"name":"Forge of Hephaestus","description":"Deal 20x fire damage to single enemy, boost ATK x1.5 for 3 turns",
         "type":"active","max_cd":14,"min_cd":8,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"single_target_damage","params":{"element":2,"multiplier":20.0,"target":"highest_hp"}},
                               {"type":"atk_buff","params":{"element":2,"multiplier":1.5,"duration_turns":3}}]}]},
        {"id":22,"name":"Athena's Aegis","description":"Defense x2.0 for 3 turns, reflect 30% damage",
         "type":"active","max_cd":15,"min_cd":10,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"defense_buff","params":{"damage_reduction":0.5,"duration_turns":3}},
                               {"type":"counter_attack","params":{"reflect_percent":0.3,"duration_turns":3}}]}]},
        {"id":23,"name":"Apollo's Radiance","description":"Heal 50% HP, change all dark gems to light",
         "type":"active","max_cd":12,"min_cd":7,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"heal_percent","params":{"percent":0.5}},
                               {"type":"gem_conversion","params":{"from_element":5,"to_element":4}}]}]},
        {"id":24,"name":"Artemis's Moonbow","description":"Deal 15x dark damage to all enemies, poison for 3 turns",
         "type":"active","max_cd":13,"min_cd":8,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"area_damage","params":{"element":5,"multiplier":15.0}},
                               {"type":"poison","params":{"multiplier":0.5,"duration_turns":3}}]}]},

        # --- Norse Gods (25-29): aggressive + combo-based ---
        {"id":25,"name":"Freyr's Tidal Blade","description":"Convert earth gems to water, boost water ATK x1.8 for 2 turns",
         "type":"active","max_cd":10,"min_cd":5,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"gem_conversion","params":{"from_element":3,"to_element":1}},
                               {"type":"atk_buff","params":{"element":1,"multiplier":1.8,"duration_turns":2}}]}]},
        {"id":26,"name":"Tyr's War Axe","description":"Deal 30x fire damage to single target, ignore defense",
         "type":"active","max_cd":15,"min_cd":10,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"absolute_damage","params":{"damage":50000,"target":"single"}}]}]},
        {"id":27,"name":"Freyja's Embrace","description":"Heal over time 80x REC for 5 turns, REC x2.0",
         "type":"active","max_cd":14,"min_cd":8,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"heal_over_time","params":{"recovery_multiplier":80.0,"duration_turns":5}},
                               {"type":"rec_buff","params":{"multiplier":2.0,"duration_turns":5}}]}]},
        {"id":28,"name":"Mjolnir Strike","description":"Deal 50000 fixed damage to all enemies",
         "type":"active","max_cd":18,"min_cd":12,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"absolute_damage","params":{"damage":50000,"target":"all"}}]}]},
        {"id":29,"name":"Loki's Mischief","description":"Change 8 random gems to dark, delay all enemies 1 turn",
         "type":"active","max_cd":10,"min_cd":5,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"random_gem_change","params":{"element":5,"count":8}},
                               {"type":"delay_enemies","params":{"turns":1,"target":"all"}}]}]},

        # --- Egyptian Gods (30-34): defensive + conditional ---
        {"id":30,"name":"Tefnut's Deluge","description":"When HP above 50%: deal 10x water damage. Below 50%: heal 30% HP",
         "type":"active","max_cd":10,"min_cd":5,"max_level":10,
         "rules":[{"conditions":[{"type":"hp_above","params":{"threshold":0.5}}],
                   "outcomes":[{"type":"area_damage","params":{"element":1,"multiplier":10.0}}]},
                  {"conditions":[{"type":"hp_below","params":{"threshold":0.5}}],
                   "outcomes":[{"type":"heal_percent","params":{"percent":0.3}}]}]},
        {"id":31,"name":"Seth's Fury","description":"Sacrifice 20% HP, deal 25x fire damage to all enemies",
         "type":"active","max_cd":12,"min_cd":7,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"self_damage","params":{"hp_percent":0.2}},
                               {"type":"area_damage","params":{"element":2,"multiplier":25.0}}]}]},
        {"id":32,"name":"Shu's Gale","description":"Spawn 6 earth orbs, reduce cooldown of all skills by 2",
         "type":"active","max_cd":12,"min_cd":7,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"orb_spawn","params":{"element":3,"count":6}},
                               {"type":"charge_skills","params":{"amount":2}}]}]},
        {"id":33,"name":"Ra's Judgment","description":"If all 5 elements matched: deal 100x light damage to all",
         "type":"active","max_cd":16,"min_cd":10,"max_level":10,
         "rules":[{"conditions":[{"type":"all_elements_matched","params":{}}],
                   "outcomes":[{"type":"area_damage","params":{"element":4,"multiplier":100.0}}]},
                  {"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"area_damage","params":{"element":4,"multiplier":10.0}}]}]},
        {"id":34,"name":"Osiris's Requiem","description":"Place time bomb on all enemies: 80000 damage after 3 turns",
         "type":"active","max_cd":15,"min_cd":10,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"time_bomb","params":{"damage":80000,"delay_turns":3}}]}]},

        # --- Chinese Gods (35-39): utility/combo ---
        {"id":35,"name":"Dragon King's Torrent","description":"Convert fire gems to water, deal 8x water damage",
         "type":"active","max_cd":10,"min_cd":5,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"gem_conversion","params":{"from_element":2,"to_element":1}},
                               {"type":"area_damage","params":{"element":1,"multiplier":8.0}}]}]},
        {"id":36,"name":"Heavenly Fire Wheels","description":"Deal 18x fire damage to all, reduce cooldowns by 3",
         "type":"active","max_cd":14,"min_cd":9,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"area_damage","params":{"element":2,"multiplier":18.0}},
                               {"type":"charge_skills","params":{"amount":3}}]}]},
        {"id":37,"name":"Bull King's Charge","description":"Deal 20x earth damage to single target, stun it",
         "type":"active","max_cd":14,"min_cd":9,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"single_target_damage","params":{"element":3,"multiplier":20.0,"target":"highest_hp"}},
                               {"type":"stun","params":{"target":"single"}}]}]},
        {"id":38,"name":"72 Transformations","description":"Change own element to enemy's weakness for 3 turns, ATK x2.0",
         "type":"active","max_cd":12,"min_cd":7,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_shift","params":{"strategy":"counter_enemy","duration_turns":3}},
                               {"type":"atk_buff","params":{"element":0,"multiplier":2.0,"duration_turns":3}}]}]},
        {"id":39,"name":"Fox Spirit's Charm","description":"Heal 40% HP, delay all enemies 2 turns, boost REC x1.5",
         "type":"active","max_cd":14,"min_cd":9,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"heal_percent","params":{"percent":0.4}},
                               {"type":"delay_enemies","params":{"turns":2,"target":"all"}},
                               {"type":"rec_buff","params":{"multiplier":1.5,"duration_turns":3}}]}]},

        # --- Chinese Beasts (40-44): element-themed utility ---
        {"id":40,"name":"Azure Dragon's Roar","description":"Spawn 5 water orbs, water ATK x1.5 for 2 turns",
         "type":"active","max_cd":10,"min_cd":5,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"orb_spawn","params":{"element":1,"count":5}},
                               {"type":"atk_buff","params":{"element":1,"multiplier":1.5,"duration_turns":2}}]}]},
        {"id":41,"name":"Vermilion Bird's Blaze","description":"Spawn 5 fire orbs, deal 10x fire damage",
         "type":"active","max_cd":10,"min_cd":5,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"orb_spawn","params":{"element":2,"count":5}},
                               {"type":"area_damage","params":{"element":2,"multiplier":10.0}}]}]},
        {"id":42,"name":"Black Tortoise's Guard","description":"Shield absorbing 30000 damage, spawn 5 earth orbs",
         "type":"active","max_cd":12,"min_cd":7,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"shield","params":{"absorb_amount":30000}},
                               {"type":"orb_spawn","params":{"element":3,"count":5}}]}]},
        {"id":43,"name":"White Tiger's Claw","description":"Deal 15x light damage to all, force drop 3 light gems",
         "type":"active","max_cd":11,"min_cd":6,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"area_damage","params":{"element":4,"multiplier":15.0}},
                               {"type":"force_drop_gem","params":{"element":4,"count":3}}]}]},
        {"id":44,"name":"Gluttonous Devour","description":"Deal 20x dark damage, lifesteal 50%",
         "type":"active","max_cd":13,"min_cd":8,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"area_damage","params":{"element":5,"multiplier":20.0}},
                               {"type":"lifesteal","params":{"percent_of_damage":0.5}}]}]},

        # --- Fodder skills (45-54): simple utility for farmable monsters ---
        {"id":45,"name":"Aqua Splash","description":"Deal 5x water damage to all enemies",
         "type":"active","max_cd":8,"min_cd":4,"max_level":5,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"area_damage","params":{"element":1,"multiplier":5.0}}]}]},
        {"id":46,"name":"Sea Song","description":"Heal 1000 HP flat",
         "type":"active","max_cd":8,"min_cd":4,"max_level":5,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"heal_flat","params":{"amount":1000}}]}]},
        {"id":47,"name":"Ember Toss","description":"Deal 5x fire damage to all enemies",
         "type":"active","max_cd":8,"min_cd":4,"max_level":5,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"area_damage","params":{"element":2,"multiplier":5.0}}]}]},
        {"id":48,"name":"Flame Dance","description":"Convert 3 random gems to fire",
         "type":"active","max_cd":8,"min_cd":4,"max_level":5,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"random_gem_change","params":{"element":2,"count":3}}]}]},
        {"id":49,"name":"Vine Whip","description":"Deal 5x earth damage to all enemies",
         "type":"active","max_cd":8,"min_cd":4,"max_level":5,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"area_damage","params":{"element":3,"multiplier":5.0}}]}]},
        {"id":50,"name":"Petal Waltz","description":"Heal 800 HP flat, spawn 2 heart orbs",
         "type":"active","max_cd":8,"min_cd":4,"max_level":5,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"heal_flat","params":{"amount":800}},
                               {"type":"orb_spawn","params":{"element":6,"count":2}}]}]},
        {"id":51,"name":"Flash Strike","description":"Deal 5x light damage to all enemies",
         "type":"active","max_cd":8,"min_cd":4,"max_level":5,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"area_damage","params":{"element":4,"multiplier":5.0}}]}]},
        {"id":52,"name":"Moonbeam","description":"Convert 3 random gems to light",
         "type":"active","max_cd":8,"min_cd":4,"max_level":5,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"random_gem_change","params":{"element":4,"count":3}}]}]},
        {"id":53,"name":"Shadow Fang","description":"Deal 5x dark damage to all enemies",
         "type":"active","max_cd":8,"min_cd":4,"max_level":5,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"area_damage","params":{"element":5,"multiplier":5.0}}]}]},
        {"id":54,"name":"Night Veil","description":"Reduce damage by 30% for 1 turn",
         "type":"active","max_cd":8,"min_cd":4,"max_level":5,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"defense_buff","params":{"damage_reduction":0.3,"duration_turns":1}}]}]},

        # --- Special/7★ skills (55-59): powerful multi-effect ---
        {"id":55,"name":"Supreme Tidal Dominion","description":"Deal 30x water damage, reduce enemy defense 50%, shield 50000",
         "type":"active","max_cd":16,"min_cd":10,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"area_damage","params":{"element":1,"multiplier":30.0}},
                               {"type":"reduce_enemy_defense","params":{"percent":0.5,"duration_turns":3}},
                               {"type":"shield","params":{"absorb_amount":50000}}]}]},
        {"id":56,"name":"Incinerating Conflagration","description":"Deal 100000 absolute damage to all, ATK x3.0 for 1 turn",
         "type":"active","max_cd":20,"min_cd":15,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"absolute_damage","params":{"damage":100000,"target":"all"}},
                               {"type":"atk_buff","params":{"element":0,"multiplier":3.0,"duration_turns":1}}]}]},
        {"id":57,"name":"Goddess Aegis Formation","description":"Counter 80% damage for 3 turns, heal over time 100x REC",
         "type":"active","max_cd":18,"min_cd":12,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"counter_attack","params":{"reflect_percent":0.8,"duration_turns":3}},
                               {"type":"heal_over_time","params":{"recovery_multiplier":100.0,"duration_turns":3}}]}]},
        {"id":58,"name":"Lustrous Fulmination","description":"If 7+ combo: deal 200x light damage to all. Otherwise 20x",
         "type":"active","max_cd":16,"min_cd":10,"max_level":10,
         "rules":[{"conditions":[{"type":"combo_above","params":{"min_combo":7}}],
                   "outcomes":[{"type":"area_damage","params":{"element":4,"multiplier":200.0}}]},
                  {"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"area_damage","params":{"element":4,"multiplier":20.0}}]}]},
        {"id":59,"name":"Corruption of Eternity","description":"Time bomb 150000 dmg in 2 turns, poison 2x for 5 turns",
         "type":"active","max_cd":18,"min_cd":12,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"time_bomb","params":{"damage":150000,"delay_turns":2}},
                               {"type":"poison","params":{"multiplier":2.0,"duration_turns":5}}]}]},

        # --- Extra condition-based skills (60): uses remaining conditions ---
        {"id":60,"name":"Turn Tide","description":"On turn 1: full skill charge. On turn 5+: deal 50x all-element damage",
         "type":"active","max_cd":8,"min_cd":4,"max_level":10,
         "rules":[{"conditions":[{"type":"turn_number","params":{"turn":1}}],
                   "outcomes":[{"type":"charge_skills","params":{"amount":99}}]},
                  {"conditions":[{"type":"turn_number","params":{"min_turn":5}}],
                   "outcomes":[{"type":"area_damage","params":{"element":0,"multiplier":50.0}}]},
                  {"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"heal_flat","params":{"amount":2000}}]}]},

        # --- Skills using remaining registered types ---
        {"id":61,"name":"Elemental Transmutation","description":"Change all water gems to fire gems (legacy format)",
         "type":"active","max_cd":8,"min_cd":4,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"change_gem_element","params":{"from":1,"to":2}}]}]},
        {"id":62,"name":"Temporal Acceleration","description":"Reduce all ally cooldowns by 3 for 2 turns",
         "type":"active","max_cd":14,"min_cd":9,"max_level":10,
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"reduce_cooldown","params":{"amount":3,"duration_turns":2}}]}]},
    ]
    return skills


def generate_leader_skills():
    """20 leader skills: existing 5 + 25 new (5 per god series)."""
    skills = [
        # --- Existing (1-5) ---
        {"id":1,"name":"Water Dragon's Might","description":"Water ATK x2.5","type":"leader",
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":1,"multiplier":2.5}}]}]},
        {"id":2,"name":"Phoenix Blaze","description":"Fire ATK x2.5, HP x1.3","type":"leader",
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":2,"multiplier":2.5}},
                               {"type":"element_hp_mult","params":{"element":2,"multiplier":1.3}}]}]},
        {"id":3,"name":"Earth's Bastion","description":"Earth HP x2.0, ATK x2.0","type":"leader",
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_hp_mult","params":{"element":3,"multiplier":2.0}},
                               {"type":"element_atk_mult","params":{"element":3,"multiplier":2.0}}]}]},
        {"id":4,"name":"Heaven's Grace","description":"Light ATK x2.0, REC x2.0","type":"leader",
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":4,"multiplier":2.0}},
                               {"type":"element_rec_mult","params":{"element":4,"multiplier":2.0}}]}]},
        {"id":5,"name":"Reaper's Bargain","description":"Dark ATK x3.0, HP x0.7","type":"leader",
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":5,"multiplier":3.0}},
                               {"type":"element_hp_mult","params":{"element":5,"multiplier":0.7}}]}]},
        # --- Greek Gods (6-10): classic mono-element leaders ---
        {"id":6,"name":"God of the Sea","description":"Water ATK x2.5, HP x1.5","type":"leader",
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":1,"multiplier":2.5}},
                               {"type":"element_hp_mult","params":{"element":1,"multiplier":1.5}}]}]},
        {"id":7,"name":"God of the Forge","description":"Fire ATK x3.0","type":"leader",
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":2,"multiplier":3.0}}]}]},
        {"id":8,"name":"Goddess of War Strategy","description":"Earth HP x2.5, ATK x2.0","type":"leader",
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_hp_mult","params":{"element":3,"multiplier":2.5}},
                               {"type":"element_atk_mult","params":{"element":3,"multiplier":2.0}}]}]},
        {"id":9,"name":"Sun God's Blessing","description":"Light ATK x2.0, REC x2.5","type":"leader",
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":4,"multiplier":2.0}},
                               {"type":"element_rec_mult","params":{"element":4,"multiplier":2.5}}]}]},
        {"id":10,"name":"Moon Huntress","description":"Dark ATK x3.5, HP x0.8","type":"leader",
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":5,"multiplier":3.5}},
                               {"type":"element_hp_mult","params":{"element":5,"multiplier":0.8}}]}]},
        # --- Norse Gods (11-15): combo-conditional ---
        {"id":11,"name":"Victorious Tides","description":"5+ combo: Water ATK x3.0","type":"leader",
         "rules":[{"conditions":[{"type":"combo_gte","params":{"min_combo":5}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":1,"multiplier":3.0}}]},
                  {"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":1,"multiplier":1.5}}]}]},
        {"id":12,"name":"One-armed Fury","description":"Fire ATK x3.5, REC x0.5","type":"leader",
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":2,"multiplier":3.5}},
                               {"type":"element_rec_mult","params":{"element":2,"multiplier":0.5}}]}]},
        {"id":13,"name":"Fair Deity's Love","description":"Earth HP x2.0, REC x2.0","type":"leader",
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_hp_mult","params":{"element":3,"multiplier":2.0}},
                               {"type":"element_rec_mult","params":{"element":3,"multiplier":2.0}}]}]},
        {"id":14,"name":"Thunder God's Wrath","description":"6+ combo: All ATK x3.0","type":"leader",
         "rules":[{"conditions":[{"type":"combo_gte","params":{"min_combo":6}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":0,"multiplier":3.0}}]},
                  {"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":0,"multiplier":1.0}}]}]},
        {"id":15,"name":"Trickster's Gambit","description":"HP below 50%: Dark ATK x4.0. Above: ATK x2.0","type":"leader",
         "rules":[{"conditions":[{"type":"hp_below","params":{"threshold":0.5}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":5,"multiplier":4.0}}]},
                  {"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":5,"multiplier":2.0}}]}]},
        # --- Egyptian Gods (16-20): HP-conditional + rainbow ---
        {"id":16,"name":"Rain Goddess Power","description":"HP above 80%: Water ATK x3.5","type":"leader",
         "rules":[{"conditions":[{"type":"hp_above","params":{"threshold":0.8}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":1,"multiplier":3.5}}]},
                  {"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":1,"multiplier":1.5}}]}]},
        {"id":17,"name":"War Deity's Resolve","description":"Fire ATK x2.5, HP x2.0","type":"leader",
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":2,"multiplier":2.5}},
                               {"type":"element_hp_mult","params":{"element":2,"multiplier":2.0}}]}]},
        {"id":18,"name":"Sky God's Domain","description":"Earth HP x3.0, ATK x1.5","type":"leader",
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_hp_mult","params":{"element":3,"multiplier":3.0}},
                               {"type":"element_atk_mult","params":{"element":3,"multiplier":1.5}}]}]},
        {"id":19,"name":"Child of the Sun","description":"All elements matched: All ATK x4.0","type":"leader",
         "rules":[{"conditions":[{"type":"all_elements_matched","params":{}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":0,"multiplier":4.0}}]},
                  {"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":0,"multiplier":1.0}}]}]},
        {"id":20,"name":"Afterlife's Embrace","description":"Dark ATK x2.5, HP x1.5, REC x1.5","type":"leader",
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":5,"multiplier":2.5}},
                               {"type":"element_hp_mult","params":{"element":5,"multiplier":1.5}},
                               {"type":"element_rec_mult","params":{"element":5,"multiplier":1.5}}]}]},
        # --- Chinese Gods & Beasts (21-30) ---
        {"id":21,"name":"Dragon King's Dominion","description":"Water HP x2.0, ATK x2.0, REC x0.5","type":"leader",
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_hp_mult","params":{"element":1,"multiplier":2.0}},
                               {"type":"element_atk_mult","params":{"element":1,"multiplier":2.0}},
                               {"type":"element_rec_mult","params":{"element":1,"multiplier":0.5}}]}]},
        {"id":22,"name":"Lotus Prince's Fire","description":"Fire ATK x3.0, HP x1.2","type":"leader",
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":2,"multiplier":3.0}},
                               {"type":"element_hp_mult","params":{"element":2,"multiplier":1.2}}]}]},
        {"id":23,"name":"Bull King's Might","description":"Earth HP x3.0","type":"leader",
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_hp_mult","params":{"element":3,"multiplier":3.0}}]}]},
        {"id":24,"name":"Great Sage's Wisdom","description":"4+ elements matched: All ATK x3.5","type":"leader",
         "rules":[{"conditions":[{"type":"elements_matched","params":{"min_elements":4}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":0,"multiplier":3.5}}]},
                  {"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":0,"multiplier":1.5}}]}]},
        {"id":25,"name":"Enchantress's Allure","description":"Dark ATK x2.0, REC x3.0","type":"leader",
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":5,"multiplier":2.0}},
                               {"type":"element_rec_mult","params":{"element":5,"multiplier":3.0}}]}]},
        {"id":26,"name":"Azure Dragon's Authority","description":"Water ATK x2.0, HP x2.0","type":"leader",
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":1,"multiplier":2.0}},
                               {"type":"element_hp_mult","params":{"element":1,"multiplier":2.0}}]}]},
        {"id":27,"name":"Vermilion Blaze","description":"Fire ATK x2.5, all REC x1.3","type":"leader",
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":2,"multiplier":2.5}},
                               {"type":"element_rec_mult","params":{"element":0,"multiplier":1.3}}]}]},
        {"id":28,"name":"Tortoise's Endurance","description":"Earth HP x2.5, ATK x1.5","type":"leader",
         "rules":[{"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_hp_mult","params":{"element":3,"multiplier":2.5}},
                               {"type":"element_atk_mult","params":{"element":3,"multiplier":1.5}}]}]},
        {"id":29,"name":"White Tiger's Strike","description":"5+ combo: Light ATK x3.5","type":"leader",
         "rules":[{"conditions":[{"type":"combo_gte","params":{"min_combo":5}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":4,"multiplier":3.5}}]},
                  {"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":4,"multiplier":1.5}}]}]},
        {"id":30,"name":"Gluttonous Power","description":"1 enemy alive: Dark ATK x4.0","type":"leader",
         "rules":[{"conditions":[{"type":"enemies_alive","params":{"max_count":1}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":5,"multiplier":4.0}}]},
                  {"conditions":[{"type":"always_true","params":{}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":5,"multiplier":2.0}}]}]},
    ]
    return skills


def generate_team_skills():
    """10 team skills: existing 5 + 5 new with synergy conditions."""
    skills = [
        # --- Existing (1-5) ---
        {"id":1,"name":"Tidal Formation","description":"All Water team: ATK x1.5 for all members","type":"team",
         "rules":[{"conditions":[{"type":"team_has_element","params":{"element":1,"require_all":True}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":0,"multiplier":1.5}}]}]},
        {"id":2,"name":"Inferno Alliance","description":"3+ Fire members: Fire ATK x2.0","type":"team",
         "rules":[{"conditions":[{"type":"team_has_element","params":{"element":2,"min_count":3}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":2,"multiplier":2.0}}]}]},
        {"id":3,"name":"Nature's Pact","description":"2+ Earth members: HP x1.5, REC x1.3","type":"team",
         "rules":[{"conditions":[{"type":"team_has_element","params":{"element":3,"min_count":2}}],
                   "outcomes":[{"type":"element_hp_mult","params":{"element":3,"multiplier":1.5}},
                               {"type":"element_rec_mult","params":{"element":3,"multiplier":1.3}}]}]},
        {"id":4,"name":"Radiant Unity","description":"All Light team: ATK x2.0, HP x1.2","type":"team",
         "rules":[{"conditions":[{"type":"team_has_element","params":{"element":4,"require_all":True}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":0,"multiplier":2.0}},
                               {"type":"element_hp_mult","params":{"element":0,"multiplier":1.2}}]}]},
        {"id":5,"name":"Shadow Covenant","description":"3+ Dark members: Dark ATK x2.5, HP x0.8","type":"team",
         "rules":[{"conditions":[{"type":"team_has_element","params":{"element":5,"min_count":3}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":5,"multiplier":2.5}},
                               {"type":"element_hp_mult","params":{"element":5,"multiplier":0.8}}]}]},
        # --- New (6-10): God-series synergies ---
        {"id":6,"name":"Olympian Pact","description":"2+ God group members: All ATK x1.3, HP x1.2","type":"team",
         "rules":[{"conditions":[{"type":"team_has_element","params":{"element":0,"min_count":2}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":0,"multiplier":1.3}},
                               {"type":"element_hp_mult","params":{"element":0,"multiplier":1.2}}]}]},
        {"id":7,"name":"Rainbow Formation","description":"All 5 elements on team: ATK x2.0","type":"team",
         "rules":[{"conditions":[{"type":"team_has_element","params":{"element":1,"min_count":1}},
                   {"type":"team_has_element","params":{"element":2,"min_count":1}},
                   {"type":"team_has_element","params":{"element":3,"min_count":1}},
                   {"type":"team_has_element","params":{"element":4,"min_count":1}},
                   {"type":"team_has_element","params":{"element":5,"min_count":1}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":0,"multiplier":2.0}}]}]},
        {"id":8,"name":"Dual Element Surge","description":"2+ Water and 2+ Fire: ATK x1.8","type":"team",
         "rules":[{"conditions":[{"type":"team_has_element","params":{"element":1,"min_count":2}},
                   {"type":"team_has_element","params":{"element":2,"min_count":2}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":0,"multiplier":1.8}}]}]},
        {"id":9,"name":"Light-Dark Harmony","description":"1+ Light and 1+ Dark: HP x1.5, REC x1.5","type":"team",
         "rules":[{"conditions":[{"type":"team_has_element","params":{"element":4,"min_count":1}},
                   {"type":"team_has_element","params":{"element":5,"min_count":1}}],
                   "outcomes":[{"type":"element_hp_mult","params":{"element":0,"multiplier":1.5}},
                               {"type":"element_rec_mult","params":{"element":0,"multiplier":1.5}}]}]},
        {"id":10,"name":"Beast Resonance","description":"3+ Beast group: ATK x1.5, HP x1.5","type":"team",
         "rules":[{"conditions":[{"type":"team_has_element","params":{"element":0,"min_count":3}}],
                   "outcomes":[{"type":"element_atk_mult","params":{"element":0,"multiplier":1.5}},
                               {"type":"element_hp_mult","params":{"element":0,"multiplier":1.5}}]}]},
    ]
    return skills


def generate_stages():
    """44 stages across all difficulty tiers."""
    stages = []

    # --- Existing Normal stages (1-6) preserved exactly ---
    existing_normal = [
        {"id":1,"name":"Aqua Temple","zone":"Aqua Temple","difficulty":"normal","stamina_cost":10,
         "waves":[{"enemies":[{"name":"Water Slime","element":1,"hp":5000,"atk":300,"defense":50,"countdown":3,"max_countdown":3},
                              {"name":"Water Slime","element":1,"hp":5000,"atk":300,"defense":50,"countdown":2,"max_countdown":3}]},
                  {"enemies":[{"name":"Water Elemental","element":1,"hp":12000,"atk":600,"defense":100,"countdown":2,"max_countdown":2,"behavior":"heavy_attack"},
                              {"name":"Water Sprite","element":1,"hp":4000,"atk":200,"defense":30,"countdown":1,"max_countdown":2}]},
                  {"enemies":[{"name":"Water Dragon Boss","element":1,"hp":50000,"atk":1500,"defense":200,"countdown":1,"max_countdown":2,"behavior":"heavy_attack"}]}],
         "rewards":[{"type":"currency","id":0,"item_id":0,"currency":"coins","count":1000},{"type":"monster","id":16,"item_id":16,"count":1}]},
        {"id":2,"name":"Fire Cavern","zone":"Fire Cavern","difficulty":"normal","stamina_cost":10,
         "waves":[{"enemies":[{"name":"Fire Lizard","element":2,"hp":5500,"atk":350,"defense":40,"countdown":2,"max_countdown":2},
                              {"name":"Fire Lizard","element":2,"hp":5500,"atk":350,"defense":40,"countdown":3,"max_countdown":3}]},
                  {"enemies":[{"name":"Lava Golem","element":2,"hp":14000,"atk":700,"defense":120,"countdown":2,"max_countdown":3,"behavior":"heal_self"},
                              {"name":"Fire Imp","element":2,"hp":3500,"atk":250,"defense":20,"countdown":1,"max_countdown":1}]},
                  {"enemies":[{"name":"Inferno Phoenix Boss","element":2,"hp":55000,"atk":1800,"defense":180,"countdown":1,"max_countdown":2,"behavior":"heavy_attack"}]}],
         "rewards":[{"type":"currency","id":0,"item_id":0,"currency":"coins","count":1100},{"type":"monster","id":17,"item_id":17,"count":1}]},
        {"id":3,"name":"Earth Forest","zone":"Earth Forest","difficulty":"normal","stamina_cost":10,
         "waves":[{"enemies":[{"name":"Treant Sapling","element":3,"hp":6000,"atk":280,"defense":70,"countdown":3,"max_countdown":3},
                              {"name":"Treant Sapling","element":3,"hp":6000,"atk":280,"defense":70,"countdown":2,"max_countdown":3},
                              {"name":"Moss Beetle","element":3,"hp":3000,"atk":200,"defense":30,"countdown":1,"max_countdown":2}]},
                  {"enemies":[{"name":"Ancient Treant","element":3,"hp":18000,"atk":550,"defense":150,"countdown":3,"max_countdown":3,"behavior":"buff_allies"}]},
                  {"enemies":[{"name":"Earth Titan Boss","element":3,"hp":60000,"atk":1200,"defense":300,"countdown":2,"max_countdown":2,"behavior":"heal_self"}]}],
         "rewards":[{"type":"currency","id":0,"item_id":0,"currency":"coins","count":1200},{"type":"monster","id":18,"item_id":18,"count":1}]},
        {"id":4,"name":"Light Shrine","zone":"Light Shrine","difficulty":"normal","stamina_cost":12,
         "waves":[{"enemies":[{"name":"Light Wisp","element":4,"hp":4500,"atk":320,"defense":45,"countdown":2,"max_countdown":2},
                              {"name":"Light Wisp","element":4,"hp":4500,"atk":320,"defense":45,"countdown":3,"max_countdown":3}]},
                  {"enemies":[{"name":"Guardian Statue","element":4,"hp":15000,"atk":500,"defense":200,"countdown":3,"max_countdown":3,"behavior":"buff_allies"},
                              {"name":"Holy Sprite","element":4,"hp":5000,"atk":400,"defense":60,"countdown":1,"max_countdown":2}]},
                  {"enemies":[{"name":"Archangel Boss","element":4,"hp":65000,"atk":1600,"defense":250,"countdown":1,"max_countdown":2,"behavior":"heavy_attack"}]}],
         "rewards":[{"type":"currency","id":0,"item_id":0,"currency":"coins","count":1300},{"type":"monster","id":19,"item_id":19,"count":1}]},
        {"id":5,"name":"Dark Abyss","zone":"Dark Abyss","difficulty":"normal","stamina_cost":15,
         "waves":[{"enemies":[{"name":"Shadow Wraith","element":5,"hp":5500,"atk":400,"defense":35,"countdown":2,"max_countdown":2},
                              {"name":"Shadow Wraith","element":5,"hp":5500,"atk":400,"defense":35,"countdown":1,"max_countdown":2}]},
                  {"enemies":[{"name":"Dark Knight","element":5,"hp":16000,"atk":800,"defense":130,"countdown":2,"max_countdown":2,"behavior":"heavy_attack"},
                              {"name":"Dark Bat","element":5,"hp":4000,"atk":350,"defense":25,"countdown":1,"max_countdown":1,"behavior":"heal_self"}]},
                  {"enemies":[{"name":"Abyssal Demon Boss","element":5,"hp":75000,"atk":2000,"defense":220,"countdown":1,"max_countdown":2,"behavior":"heavy_attack"}]}],
         "rewards":[{"type":"currency","id":0,"item_id":0,"currency":"coins","count":1500},{"type":"monster","id":20,"item_id":20,"count":1}]},
        {"id":6,"name":"Tower of Trials","zone":"Tower of Trials","difficulty":"normal","stamina_cost":20,"turn_limit":15,
         "waves":[{"enemies":[{"name":"Trial Guardian","element":4,"hp":20000,"atk":600,"defense":150,"countdown":2,"max_countdown":2},
                              {"name":"Trial Guardian","element":5,"hp":20000,"atk":600,"defense":150,"countdown":3,"max_countdown":3}]},
                  {"enemies":[{"name":"Grand Arbiter","element":4,"hp":100000,"atk":2500,"defense":400,"countdown":2,"max_countdown":2,"behavior":"heavy_attack",
                               "characteristics":{"damage_cap":50000}}]}],
         "rewards":[{"type":"currency","id":0,"item_id":0,"currency":"coins","count":3000},{"type":"currency","id":0,"item_id":0,"currency":"gems","count":5}]},
    ]
    stages.extend(existing_normal)

    # --- New Normal stages (7-16): Greek Gods, Norse Gods themed ---
    new_normal_data = [
        (7, "Olympus Gate", "Olympus", 12, 1, "Olympian Guard", 8000, 450, 80, "Sea Nymph", 4500, 300, 40, "Poseidon's Herald", 70000, 1800, 250),
        (8, "Olympus Forge", "Olympus", 12, 2, "Fire Automaton", 8500, 500, 70, "Forge Golem", 5000, 350, 50, "Hephaestus's Construct", 72000, 1900, 230),
        (9, "Athena's Garden", "Olympus", 14, 3, "Olive Dryad", 7500, 380, 90, "Wisdom Owl", 5500, 320, 60, "Gorgon Guardian", 68000, 1700, 280),
        (10, "Apollo's Arena", "Olympus", 14, 4, "Sun Sprite", 7000, 420, 55, "Golden Chariot", 6000, 400, 45, "Sun Colossus", 75000, 2000, 200),
        (11, "Artemis's Hunt", "Olympus", 15, 5, "Moon Wolf", 8000, 480, 50, "Shadow Stag", 5500, 420, 35, "Lunar Hydra", 80000, 2200, 180),
        (12, "Asgard Bridge", "Asgard", 14, 1, "Frost Giant", 9000, 500, 100, "Ice Valkyrie", 6000, 380, 70, "Bifrost Guardian", 85000, 2100, 260),
        (13, "Valhalla Gate", "Asgard", 14, 2, "Fire Jotunn", 9500, 550, 80, "Berserker", 6500, 450, 40, "Einherjar Champion", 88000, 2300, 220),
        (14, "World Tree Root", "Asgard", 15, 3, "Root Serpent", 10000, 480, 110, "Bark Guardian", 7000, 350, 90, "Nidhogg Spawn", 90000, 2000, 300),
        (15, "Thunder Hall", "Asgard", 16, 4, "Storm Eagle", 8500, 520, 60, "Lightning Elemental", 6000, 460, 50, "Thunder Golem", 82000, 2400, 190),
        (16, "Twilight Realm", "Asgard", 18, 5, "Shadow Jotunn", 9000, 580, 45, "Chaos Sprite", 5500, 500, 30, "Fenrir Pup", 95000, 2500, 170),
    ]
    for sid, sname, szone, stam, elem, e1n, e1h, e1a, e1d, e2n, e2h, e2a, e2d, bn, bh, ba, bd in new_normal_data:
        stages.append({
            "id":sid,"name":sname,"zone":szone,"difficulty":"normal","stamina_cost":stam,
            "waves":[
                {"enemies":[{"name":e1n,"element":elem,"hp":e1h,"atk":e1a,"defense":e1d,"countdown":2,"max_countdown":3},
                            {"name":e1n,"element":elem,"hp":e1h,"atk":e1a,"defense":e1d,"countdown":3,"max_countdown":3}]},
                {"enemies":[{"name":e2n,"element":elem,"hp":e2h,"atk":e2a,"defense":e2d,"countdown":2,"max_countdown":2,"behavior":"buff_allies"}]},
                {"enemies":[{"name":bn,"element":elem,"hp":bh,"atk":ba,"defense":bd,"countdown":1,"max_countdown":2,"behavior":"heavy_attack"}]}
            ],
            "rewards":[{"type":"currency","id":0,"item_id":0,"currency":"coins","count":1500+sid*100},
                       {"type":"monster","id":200+elem,"item_id":200+elem,"count":1}]
        })

    # --- Existing Expert stages (101-102) + new (103-112) ---
    existing_expert = [
        {"id":101,"name":"Aqua Temple","zone":"Aqua Temple","difficulty":"expert","stamina_cost":25,
         "waves":[{"enemies":[{"name":"Water Elemental","element":1,"hp":25000,"atk":800,"defense":150,"countdown":2,"max_countdown":2},
                              {"name":"Water Elemental","element":1,"hp":25000,"atk":800,"defense":150,"countdown":3,"max_countdown":3},
                              {"name":"Water Sprite","element":1,"hp":12000,"atk":500,"defense":80,"countdown":1,"max_countdown":2}]},
                  {"enemies":[{"name":"Abyssal Serpent","element":1,"hp":80000,"atk":1500,"defense":250,"countdown":2,"max_countdown":2,"behavior":"heavy_attack"},
                              {"name":"Water Enchantress","element":1,"hp":30000,"atk":600,"defense":100,"countdown":2,"max_countdown":3,"behavior":"jammer_spawn"}]},
                  {"enemies":[{"name":"Leviathan","element":1,"hp":200000,"atk":4000,"defense":500,"countdown":1,"max_countdown":2,"behavior":"heavy_attack",
                               "characteristics":{"combo_shield":3}}]}],
         "rewards":[{"type":"currency","id":0,"item_id":0,"currency":"coins","count":5000},
                    {"type":"currency","id":0,"item_id":0,"currency":"gems","count":1},
                    {"type":"monster","id":1,"item_id":1,"count":1}]},
        {"id":102,"name":"Fire Cavern","zone":"Fire Cavern","difficulty":"expert","stamina_cost":25,"floor_effects":["no_heart_heal"],
         "waves":[{"enemies":[{"name":"Magma Golem","element":2,"hp":30000,"atk":900,"defense":180,"countdown":2,"max_countdown":2,"behavior":"heavy_attack"},
                              {"name":"Flame Dancer","element":2,"hp":18000,"atk":700,"defense":60,"countdown":1,"max_countdown":2}]},
                  {"enemies":[{"name":"Volcanic Dragon","element":2,"hp":90000,"atk":2000,"defense":300,"countdown":2,"max_countdown":2,"behavior":"heavy_attack"},
                              {"name":"Fire Shaman","element":2,"hp":25000,"atk":500,"defense":120,"countdown":3,"max_countdown":3,"behavior":"poison_spawn"}]},
                  {"enemies":[{"name":"Ifrit","element":2,"hp":250000,"atk":5000,"defense":450,"countdown":1,"max_countdown":2,"behavior":"heavy_attack",
                               "characteristics":{"damage_reduction":0.3}}]}],
         "rewards":[{"type":"currency","id":0,"item_id":0,"currency":"coins","count":5500},
                    {"type":"currency","id":0,"item_id":0,"currency":"gems","count":1},
                    {"type":"monster","id":2,"item_id":2,"count":1}]},
    ]
    stages.extend(existing_expert)

    # New expert stages (103-112)
    expert_zones = [
        (103,"Earth Forest","Earth Forest",3), (104,"Light Shrine","Light Shrine",4),
        (105,"Dark Abyss","Dark Abyss",5), (106,"Olympus Gate","Olympus",1),
        (107,"Olympus Forge","Olympus",2), (108,"Athena's Garden","Olympus",3),
        (109,"Asgard Bridge","Asgard",1), (110,"Valhalla Gate","Asgard",2),
        (111,"World Tree Root","Asgard",3), (112,"Thunder Hall","Asgard",4),
    ]
    for sid, sname, szone, elem in expert_zones:
        bossHP = 180000 + (sid-103)*8000
        stages.append({
            "id":sid,"name":sname,"zone":szone,"difficulty":"expert","stamina_cost":25+(sid-103),
            "waves":[
                {"enemies":[{"name":f"Elite {sname} Guard","element":elem,"hp":28000+(sid-103)*2000,"atk":900+(sid-103)*50,"defense":160+(sid-103)*10,
                             "countdown":2,"max_countdown":2},
                            {"name":f"{sname} Sentinel","element":elem,"hp":20000+(sid-103)*1500,"atk":700+(sid-103)*40,"defense":100+(sid-103)*8,
                             "countdown":1,"max_countdown":2}]},
                {"enemies":[{"name":f"{sname} Champion","element":elem,"hp":80000+(sid-103)*5000,"atk":2000+(sid-103)*100,"defense":280+(sid-103)*15,
                             "countdown":2,"max_countdown":2,"behavior":"heavy_attack"}]},
                {"enemies":[{"name":f"{sname} Lord","element":elem,"hp":bossHP,"atk":3500+(sid-103)*150,"defense":400+(sid-103)*20,
                             "countdown":1,"max_countdown":2,"behavior":"heavy_attack",
                             "characteristics":{"combo_shield":3}}]}
            ],
            "rewards":[{"type":"currency","id":0,"item_id":0,"currency":"coins","count":5000+(sid-103)*200},
                       {"type":"currency","id":0,"item_id":0,"currency":"gems","count":1},
                       {"type":"monster","id":20+((sid-103)%5)+1,"item_id":20+((sid-103)%5)+1,"count":1}]
        })

    # --- Existing Mythical (201) + new (202-206) ---
    existing_mythical = [
        {"id":201,"name":"Aqua Temple","zone":"Aqua Temple","difficulty":"mythical","stamina_cost":50,
         "turn_limit":20,"board_rows":7,"board_cols":6,"floor_effects":["no_active_skills","enemy_hp_regen"],
         "waves":[{"enemies":[{"name":"Deep Sea Guardian","element":1,"hp":80000,"atk":2000,"defense":400,"countdown":2,"max_countdown":2,"behavior":"heavy_attack"},
                              {"name":"Tidal Enchantress","element":1,"hp":50000,"atk":1200,"defense":200,"countdown":2,"max_countdown":3,"behavior":"buff_allies"}]},
                  {"enemies":[{"name":"Abyssal Kraken","element":1,"hp":500000,"atk":8000,"defense":800,"countdown":1,"max_countdown":2,"behavior":"lock_gems",
                               "characteristics":{"combo_shield":4,"damage_cap":100000}}]}],
         "rewards":[{"type":"currency","id":0,"item_id":0,"currency":"coins","count":15000},
                    {"type":"currency","id":0,"item_id":0,"currency":"gems","count":3},
                    {"type":"monster","id":11,"item_id":11,"count":1}]},
    ]
    stages.extend(existing_mythical)

    mythical_data = [
        (202,"Fire Cavern","Fire Cavern",2,"Ifrit Overlord",550000,9000,900,"Volcanic Titan",90000,2500,500),
        (203,"Earth Forest","Earth Forest",3,"World Treant",600000,7000,1200,"Ancient Root Dragon",100000,2200,600),
        (204,"Olympus Sanctum","Olympus",4,"Apollo Supreme",480000,8500,700,"Sun Champion",85000,2800,400),
        (205,"Asgard Throne","Asgard",5,"Odin's Shadow",520000,9500,850,"Ragnarok Herald",95000,3000,500),
        (206,"Pyramid of Ra","Egypt",4,"Ra Incarnate",500000,10000,750,"Anubis Guardian",90000,2600,550),
    ]
    for sid,sn,sz,elem,bn,bh,ba,bd,mn,mh,ma,md in mythical_data:
        stages.append({
            "id":sid,"name":sn,"zone":sz,"difficulty":"mythical","stamina_cost":50,"turn_limit":25,
            "waves":[
                {"enemies":[{"name":mn,"element":elem,"hp":mh,"atk":ma,"defense":md,"countdown":2,"max_countdown":2,"behavior":"heavy_attack"},
                            {"name":f"{sz} Sentinel","element":elem,"hp":60000,"atk":1800,"defense":350,"countdown":2,"max_countdown":3,"behavior":"buff_allies"}]},
                {"enemies":[{"name":bn,"element":elem,"hp":bh,"atk":ba,"defense":bd,"countdown":1,"max_countdown":2,"behavior":"heavy_attack",
                             "characteristics":{"combo_shield":4,"damage_cap":120000}}]}
            ],
            "rewards":[{"type":"currency","id":0,"item_id":0,"currency":"coins","count":15000+(sid-201)*2000},
                       {"type":"currency","id":0,"item_id":0,"currency":"gems","count":3},
                       {"type":"monster","id":50+((sid-201)%5)+1,"item_id":50+((sid-201)%5)+1,"count":1}]
        })

    # --- Daily Material stages (301-305) preserved ---
    daily = [
        {"id":301,"name":"Water Material","zone":"Daily Dungeons","difficulty":"normal","stamina_cost":15,
         "available_days":[1,0,6],"floor_effects":["element_restrict_1"],
         "waves":[{"enemies":[{"name":"Water Golem","element":1,"hp":15000,"atk":500,"defense":100,"countdown":2,"max_countdown":2},
                              {"name":"Water Golem","element":1,"hp":15000,"atk":500,"defense":100,"countdown":3,"max_countdown":3}]},
                  {"enemies":[{"name":"Water Guardian","element":1,"hp":40000,"atk":1000,"defense":200,"countdown":2,"max_countdown":2,"behavior":"heavy_attack"}]}],
         "rewards":[{"type":"currency","id":0,"item_id":0,"currency":"coins","count":3000},{"type":"monster","id":104,"item_id":104,"count":1}]},
        {"id":302,"name":"Fire Material","zone":"Daily Dungeons","difficulty":"normal","stamina_cost":15,
         "available_days":[2,0,6],"floor_effects":["element_restrict_2"],
         "waves":[{"enemies":[{"name":"Fire Golem","element":2,"hp":15000,"atk":550,"defense":80,"countdown":2,"max_countdown":2},
                              {"name":"Fire Golem","element":2,"hp":15000,"atk":550,"defense":80,"countdown":3,"max_countdown":3}]},
                  {"enemies":[{"name":"Fire Guardian","element":2,"hp":42000,"atk":1100,"defense":180,"countdown":2,"max_countdown":2,"behavior":"heavy_attack"}]}],
         "rewards":[{"type":"currency","id":0,"item_id":0,"currency":"coins","count":3000},{"type":"monster","id":105,"item_id":105,"count":1}]},
        {"id":303,"name":"Earth Material","zone":"Daily Dungeons","difficulty":"normal","stamina_cost":15,
         "available_days":[3,0,6],"floor_effects":["element_restrict_3"],
         "waves":[{"enemies":[{"name":"Earth Golem","element":3,"hp":16000,"atk":480,"defense":130,"countdown":2,"max_countdown":2},
                              {"name":"Earth Golem","element":3,"hp":16000,"atk":480,"defense":130,"countdown":3,"max_countdown":3}]},
                  {"enemies":[{"name":"Earth Guardian","element":3,"hp":45000,"atk":900,"defense":250,"countdown":2,"max_countdown":2,"behavior":"heal_self"}]}],
         "rewards":[{"type":"currency","id":0,"item_id":0,"currency":"coins","count":3000},{"type":"monster","id":106,"item_id":106,"count":1}]},
        {"id":304,"name":"Light Material","zone":"Daily Dungeons","difficulty":"normal","stamina_cost":15,
         "available_days":[4,0,6],"floor_effects":["element_restrict_4"],
         "waves":[{"enemies":[{"name":"Light Golem","element":4,"hp":14000,"atk":520,"defense":90,"countdown":2,"max_countdown":2},
                              {"name":"Light Golem","element":4,"hp":14000,"atk":520,"defense":90,"countdown":3,"max_countdown":3}]},
                  {"enemies":[{"name":"Light Guardian","element":4,"hp":38000,"atk":1050,"defense":200,"countdown":2,"max_countdown":2,"behavior":"buff_allies"}]}],
         "rewards":[{"type":"currency","id":0,"item_id":0,"currency":"coins","count":3000},{"type":"monster","id":107,"item_id":107,"count":1}]},
        {"id":305,"name":"Dark Material","zone":"Daily Dungeons","difficulty":"normal","stamina_cost":15,
         "available_days":[5,0,6],"floor_effects":["element_restrict_5"],
         "waves":[{"enemies":[{"name":"Dark Golem","element":5,"hp":14500,"atk":580,"defense":70,"countdown":2,"max_countdown":2},
                              {"name":"Dark Golem","element":5,"hp":14500,"atk":580,"defense":70,"countdown":3,"max_countdown":3}]},
                  {"enemies":[{"name":"Dark Guardian","element":5,"hp":40000,"atk":1200,"defense":170,"countdown":2,"max_countdown":2,"behavior":"heavy_attack"}]}],
         "rewards":[{"type":"currency","id":0,"item_id":0,"currency":"coins","count":3000},{"type":"monster","id":108,"item_id":108,"count":1}]},
    ]
    stages.extend(daily)

    # --- Technical/Challenge stages (401-405): new ---
    tech_data = [
        (401,"Egyptian Trial","Egypt",1,"Sphinx","Only Water/Light teams",["element_restrict_1"],150000,4000,500),
        (402,"Norse Gauntlet","Asgard",2,"Surtr","Turn limit 10",["no_heart_heal"],180000,5000,400),
        (403,"Olympian Challenge","Olympus",3,"Cerberus","No active skills",["no_active_skills"],200000,4500,600),
        (404,"Celestial Duel","Tower of Trials",4,"Seraphim","Combo shield 5",None,220000,5500,350),
        (405,"Abyss Descent","Dark Abyss",5,"Abyssal Emperor","Damage cap + regen",["enemy_hp_regen"],250000,6000,450),
    ]
    for sid,sn,sz,elem,bn,desc,fe,bh,ba,bd in tech_data:
        s = {"id":sid,"name":sn,"zone":sz,"difficulty":"technical","stamina_cost":40,"turn_limit":15,
             "waves":[
                 {"enemies":[{"name":f"{sn} Guard A","element":elem,"hp":50000,"atk":2000,"defense":300,"countdown":2,"max_countdown":2,"behavior":"heavy_attack"},
                             {"name":f"{sn} Guard B","element":(elem%5)+1,"hp":45000,"atk":1800,"defense":250,"countdown":3,"max_countdown":3}]},
                 {"enemies":[{"name":bn,"element":elem,"hp":bh,"atk":ba,"defense":bd,"countdown":1,"max_countdown":2,"behavior":"heavy_attack",
                              "characteristics":{"combo_shield":4,"damage_cap":80000}}]}
             ],
             "rewards":[{"type":"currency","id":0,"item_id":0,"currency":"coins","count":10000},
                        {"type":"currency","id":0,"item_id":0,"currency":"gems","count":5},
                        {"type":"monster","id":109,"item_id":109,"count":1}]}
        if fe:
            s["floor_effects"] = fe
        stages.append(s)

    return stages


def generate_gacha_pools(all_monster_ids):
    """10 gacha pools."""
    pools = [
        # Existing pools (1-4) updated with new monster IDs
        {"id":1,"name":"Standard Pool","cost_currency":"gems","cost_amount":5,"pity_threshold":50,"daily_free":True,"multi_pull_discount":1,
         "entries":[
             *[{"monster_id":m,"rarity":5,"weight":5,"is_featured":True} for m in [1,2,3,4,5]],
             *[{"monster_id":m,"rarity":5,"weight":3,"is_featured":False} for m in range(21,46)],
             *[{"monster_id":m,"rarity":4,"weight":12,"is_featured":False} for m in [6,7,8,9,10]],
             *[{"monster_id":m,"rarity":3,"weight":30,"is_featured":False} for m in [16,17,18,19,20]],
         ]},
        {"id":2,"name":"Fire Festival","cost_currency":"gems","cost_amount":5,"pity_threshold":30,
         "featured_ids":[2,22,27],
         "entries":[
             {"monster_id":2,"rarity":5,"weight":15,"is_featured":True},
             {"monster_id":22,"rarity":5,"weight":10,"is_featured":True},
             {"monster_id":27,"rarity":5,"weight":10,"is_featured":True},
             *[{"monster_id":m,"rarity":5,"weight":3,"is_featured":False} for m in [1,3,4,5,21,23,24,25]],
             *[{"monster_id":m,"rarity":4,"weight":12,"is_featured":False} for m in [6,7,8,9,10]],
             *[{"monster_id":m,"rarity":3,"weight":30,"is_featured":False} for m in [16,17,18,19,20]],
         ]},
        {"id":3,"name":"Dragon Festival Step-Up","cost_currency":"gems","cost_amount":5,"pity_threshold":30,
         "step_up":[
             {"cost_mult":0.0,"pull_count":5,"guaranteed_rarity":0},
             {"cost_mult":0.5,"pull_count":10,"guaranteed_rarity":4},
             {"cost_mult":1.0,"pull_count":10,"guaranteed_rarity":4},
             {"cost_mult":1.0,"pull_count":10,"guaranteed_rarity":5},
         ],
         "entries":[
             *[{"monster_id":m,"rarity":5,"weight":6,"is_featured":True} for m in [1,3]],
             *[{"monster_id":m,"rarity":5,"weight":4,"is_featured":False} for m in [2,4,5]],
             *[{"monster_id":m,"rarity":4,"weight":12,"is_featured":False} for m in [6,7,8,9,10]],
             *[{"monster_id":m,"rarity":3,"weight":30,"is_featured":False} for m in [16,17,18,19,20]],
         ]},
        {"id":4,"name":"Beginner's Choice","cost_currency":"gems","cost_amount":3,"one_time":True,"guaranteed_top_rarity":True,
         "entries":[
             *[{"monster_id":m,"rarity":5,"weight":20,"is_featured":True} for m in [1,2,3,4,5]],
             *[{"monster_id":m,"rarity":4,"weight":15,"is_featured":False} for m in [6,7,8,9,10]],
         ]},
        # New pools (5-10): god-series themed
        {"id":5,"name":"Greek Gods Seal","cost_currency":"gems","cost_amount":5,"pity_threshold":30,
         "featured_ids":[21,22,23,24,25],
         "entries":[
             *[{"monster_id":m,"rarity":5,"weight":12,"is_featured":True} for m in [21,22,23,24,25]],
             *[{"monster_id":m,"rarity":5,"weight":3,"is_featured":False} for m in [1,2,3,4,5]],
             *[{"monster_id":m,"rarity":4,"weight":10,"is_featured":False} for m in [6,7,8,9,10]],
             *[{"monster_id":m,"rarity":3,"weight":25,"is_featured":False} for m in [16,17,18,19,20]],
         ]},
        {"id":6,"name":"Norse Gods Seal","cost_currency":"gems","cost_amount":5,"pity_threshold":30,
         "featured_ids":[26,27,28,29,30],
         "entries":[
             *[{"monster_id":m,"rarity":5,"weight":12,"is_featured":True} for m in [26,27,28,29,30]],
             *[{"monster_id":m,"rarity":5,"weight":3,"is_featured":False} for m in [1,2,3,4,5]],
             *[{"monster_id":m,"rarity":4,"weight":10,"is_featured":False} for m in [6,7,8,9,10]],
             *[{"monster_id":m,"rarity":3,"weight":25,"is_featured":False} for m in [16,17,18,19,20]],
         ]},
        {"id":7,"name":"Egyptian Gods Seal","cost_currency":"gems","cost_amount":5,"pity_threshold":30,
         "featured_ids":[31,32,33,34,35],
         "entries":[
             *[{"monster_id":m,"rarity":5,"weight":12,"is_featured":True} for m in [31,32,33,34,35]],
             *[{"monster_id":m,"rarity":5,"weight":3,"is_featured":False} for m in [1,2,3,4,5]],
             *[{"monster_id":m,"rarity":4,"weight":10,"is_featured":False} for m in [6,7,8,9,10]],
             *[{"monster_id":m,"rarity":3,"weight":25,"is_featured":False} for m in [16,17,18,19,20]],
         ]},
        {"id":8,"name":"Chinese Legends Seal","cost_currency":"gems","cost_amount":5,"pity_threshold":30,
         "featured_ids":[36,37,38,39,40],
         "entries":[
             *[{"monster_id":m,"rarity":5,"weight":12,"is_featured":True} for m in [36,37,38,39,40]],
             *[{"monster_id":m,"rarity":5,"weight":3,"is_featured":False} for m in [41,42,43,44,45]],
             *[{"monster_id":m,"rarity":4,"weight":10,"is_featured":False} for m in [6,7,8,9,10]],
             *[{"monster_id":m,"rarity":3,"weight":25,"is_featured":False} for m in [16,17,18,19,20]],
         ]},
        {"id":9,"name":"Primal Gods Seal","cost_currency":"gems","cost_amount":5,"pity_threshold":20,
         "featured_ids":[301,302,303,304,305],
         "entries":[
             *[{"monster_id":m,"rarity":7,"weight":3,"is_featured":True} for m in [301,302,303,304,305]],
             *[{"monster_id":m,"rarity":5,"weight":5,"is_featured":False} for m in range(21,46)],
             *[{"monster_id":m,"rarity":4,"weight":10,"is_featured":False} for m in [6,7,8,9,10]],
             *[{"monster_id":m,"rarity":3,"weight":25,"is_featured":False} for m in [16,17,18,19,20]],
         ]},
        {"id":10,"name":"Water Rate-Up","cost_currency":"gems","cost_amount":5,"pity_threshold":30,
         "featured_ids":[1,21,26,31,36],
         "entries":[
             *[{"monster_id":m,"rarity":5,"weight":15,"is_featured":True} for m in [1,21,26,31,36]],
             *[{"monster_id":m,"rarity":5,"weight":2,"is_featured":False} for m in [2,3,4,5]],
             {"monster_id":6,"rarity":4,"weight":20,"is_featured":True},
             *[{"monster_id":m,"rarity":4,"weight":8,"is_featured":False} for m in [7,8,9,10]],
             {"monster_id":16,"rarity":3,"weight":40,"is_featured":True},
             *[{"monster_id":m,"rarity":3,"weight":20,"is_featured":False} for m in [17,18,19,20]],
         ]},
    ]
    return pools


def generate_loot_tables(stages):
    """One loot table per non-daily stage."""
    tables = []
    tid = 1
    for stage in stages:
        if stage.get("available_days"):
            continue  # daily stages have their own drop logic via rewards
        elem = 1
        if stage["waves"] and stage["waves"][0]["enemies"]:
            elem = stage["waves"][0]["enemies"][0].get("element", 1)
        diff_mult = {"normal":1,"expert":2,"mythical":4,"technical":3}.get(stage["difficulty"],1)
        entries = [
            {"type":"currency","item_id":0,"currency":"coins",
             "count_min":500*diff_mult,"count_max":1500*diff_mult,"weight":100,"guaranteed":True},
        ]
        # Element-matching fodder drop
        fodder_id = 200 + elem
        if fodder_id <= 225:
            entries.append({"type":"monster","item_id":fodder_id,"count_min":1,"count_max":1,"weight":40,"guaranteed":False})
        # Evo material drop
        mat_id = 103 + elem
        if mat_id <= 115:
            entries.append({"type":"monster","item_id":mat_id,"count_min":1,"count_max":1,"weight":25//diff_mult + 5,"guaranteed":False})
        # Rare gem drop for expert+
        if diff_mult >= 2:
            entries.append({"type":"currency","item_id":0,"currency":"gems","count_min":1,"count_max":diff_mult,"weight":5*diff_mult,"guaranteed":False})
        tables.append({"id":tid,"stage_id":stage["id"],"entries":entries})
        tid += 1
    return tables


def generate_monster_exchange(all_monsters):
    """Expanded exchange list for 5★+ monsters."""
    exchanges = []
    eid = 1
    for m in all_monsters:
        if m["rarity"] >= 5 and m["rarity"] <= 6 and m.get("evolve_to") is not None:
            # Only offer base forms for exchange (they can be evolved by the player)
            exchanges.append({
                "id":eid,"name":f"{m['name']} Exchange","target_monster_id":m["id"],
                "required_count":5,"required_min_rarity":4,"required_element":0,"exchange_limit":1
            })
            eid += 1
    # Also keep 7★ exchange at higher cost
    for m in all_monsters:
        if m["rarity"] == 7:
            exchanges.append({
                "id":eid,"name":f"{m['name']} Exchange","target_monster_id":m["id"],
                "required_count":8,"required_min_rarity":5,"required_element":0,"exchange_limit":1
            })
            eid += 1
    return exchanges


def generate_event_shops(all_monsters):
    """Expanded event shop with god series monsters."""
    shops = [
        {"id":1,"name":"Dragon Festival Exchange","currency":"event_tokens",
         "items":[
             {"id":1,"name":"Water Dragon","type":"monster","item_id":1,"count":1,
              "cost_currency":"event_tokens","cost_amount":300,"buy_limit":1},
             {"id":2,"name":"Fire Phoenix","type":"monster","item_id":2,"count":1,
              "cost_currency":"event_tokens","cost_amount":300,"buy_limit":1},
             {"id":3,"name":"Elemental Shard x5","type":"monster","item_id":101,"count":5,
              "cost_currency":"event_tokens","cost_amount":50,"buy_limit":10},
             {"id":4,"name":"10,000 Coins","type":"currency","currency_reward":"coins","count":10000,
              "cost_currency":"event_tokens","cost_amount":20,"buy_limit":0},
             {"id":5,"name":"5 Gems","type":"currency","currency_reward":"gems","count":5,
              "cost_currency":"event_tokens","cost_amount":100,"buy_limit":3},
         ]},
        {"id":2,"name":"God Series Exchange","currency":"event_tokens",
         "items":[
             *[{"id":10+i,"name":m["name"],"type":"monster","item_id":m["id"],"count":1,
                "cost_currency":"event_tokens","cost_amount":500,"buy_limit":1}
               for i,m in enumerate([m for m in all_monsters if m["id"] in [21,22,23,24,25]])],
             {"id":20,"name":"Spirit Jewel x3","type":"monster","item_id":109,"count":3,
              "cost_currency":"event_tokens","cost_amount":80,"buy_limit":5},
             {"id":21,"name":"Water Evo Soul x5","type":"monster","item_id":104,"count":5,
              "cost_currency":"event_tokens","cost_amount":40,"buy_limit":10},
             {"id":22,"name":"15 Gems","type":"currency","currency_reward":"gems","count":15,
              "cost_currency":"event_tokens","cost_amount":200,"buy_limit":2},
         ]},
    ]
    return shops


def generate_player_levels():
    """500-level progression table based on ToS stamina/exp curves."""
    levels = []
    for level in range(1, 501):
        # Authentic ToS-style exp curve: roughly quadratic
        exp_to_next = int(100 * level + 50 * (level ** 1.5))
        # Stamina grows: starts at 20, +1 every 2 levels, cap at 300
        stamina = min(20 + level // 2, 300)
        # Team cost grows: starts at 30, +2 per level
        team_cost = 30 + level * 2
        # Friend slots: starts at 20, +1 every 5 levels, cap 100
        friend_slots = min(20 + level // 5, 100)
        levels.append({
            "id": level,
            "level": level,
            "exp_to_next": exp_to_next,
            "stamina": stamina,
            "team_cost": team_cost,
            "friend_slots": friend_slots
        })
    return levels


# ==============================================================================
# Validation
# ==============================================================================

def validate(data_dir):
    """Validate all data file cross-references."""
    errors = []

    def load(name):
        path = os.path.join(data_dir, f"{name}.json")
        if not os.path.exists(path):
            errors.append(f"Missing file: {name}.json")
            return []
        with open(path) as f:
            return json.load(f)

    monsters = load("monsters")
    skills = load("skills")
    leader_skills = load("leader_skills")
    stages = load("stages")
    gacha_pools = load("gacha_pools")

    monster_ids = {m["id"] for m in monsters}
    skill_ids = {s["id"] for s in skills}
    leader_skill_ids = {s["id"] for s in leader_skills}

    # Check monster skill references
    for m in monsters:
        sid = m.get("active_skill_id")
        if sid is not None and sid not in skill_ids:
            errors.append(f"Monster {m['id']} ({m['name']}): active_skill_id {sid} not found")
        lid = m.get("leader_skill_id")
        if lid is not None and lid not in leader_skill_ids:
            errors.append(f"Monster {m['id']} ({m['name']}): leader_skill_id {lid} not found")
        evo = m.get("evolve_to")
        if evo is not None and evo not in monster_ids:
            errors.append(f"Monster {m['id']} ({m['name']}): evolve_to {evo} not found")
        for mat in m.get("evolve_materials", []):
            if mat not in monster_ids:
                errors.append(f"Monster {m['id']} ({m['name']}): evolve_material {mat} not found")
        if m.get("element", 0) not in range(1, 7):
            errors.append(f"Monster {m['id']} ({m['name']}): invalid element {m.get('element')}")

    # Check gacha pool monster references
    for pool in gacha_pools:
        for entry in pool.get("entries", []):
            mid = entry.get("monster_id")
            if mid not in monster_ids:
                errors.append(f"Gacha pool {pool['id']} ({pool['name']}): monster_id {mid} not found")

    # Check stage enemy elements
    for stage in stages:
        for wi, wave in enumerate(stage.get("waves", [])):
            for ei, enemy in enumerate(wave.get("enemies", [])):
                if enemy.get("element", 0) not in range(1, 7):
                    errors.append(f"Stage {stage['id']} wave {wi} enemy {ei}: invalid element {enemy.get('element')}")

    # Registered types from RegisterAll.cs
    REGISTERED_CONDITIONS = {
        "always_true","combo_above","hp_below","elements_matched","team_has_element",
        "combo_gte","combo_lt","hp_above","turn_number","enemies_alive","all_elements_matched"
    }
    REGISTERED_EFFECTS = {
        "area_damage","heal_flat","heal_percent","change_gem_element","delay_enemies",
        "single_target_damage","gem_conversion","self_damage","element_change","rec_buff",
        "lifesteal","element_atk_mult","element_hp_mult","element_rec_mult",
        "absolute_damage","charge_skills","random_gem_change","shield","time_bomb"
    }
    REGISTERED_OUTCOMES = {
        "heal_over_time","atk_buff","defense_buff","combo_scaling_atk",
        "reduce_enemy_defense","counter_attack","reduce_cooldown","element_shift","poison","force_drop_gem"
    }
    ALL_KNOWN = REGISTERED_CONDITIONS | REGISTERED_EFFECTS | REGISTERED_OUTCOMES
    # Also allow these from existing data (not registered but used in JSON)
    EXTRA_TYPES = {"bind","poison_dot","stun","gravity_damage","void_damage_absorb","void_element_shield","orb_spawn"}
    ALL_KNOWN |= EXTRA_TYPES

    # Check that all registered types appear in at least one skill
    used_types = set()
    for skill_list in [skills, leader_skills, load("team_skills")]:
        for skill in skill_list:
            for rule in skill.get("rules", []):
                for cond in rule.get("conditions", []):
                    used_types.add(cond["type"])
                for out in rule.get("outcomes", []):
                    used_types.add(out["type"])

    missing_registered = (REGISTERED_CONDITIONS | REGISTERED_EFFECTS | REGISTERED_OUTCOMES) - used_types
    if missing_registered:
        errors.append(f"Registered types never used in any skill: {missing_registered}")

    if errors:
        print(f"VALIDATION FAILED — {len(errors)} error(s):")
        for e in errors:
            print(f"  X {e}")
        return False
    else:
        print(f"VALIDATION PASSED")
        print(f"  Monsters: {len(monsters)}")
        print(f"  Active Skills: {len(skills)}")
        print(f"  Leader Skills: {len(leader_skills)}")
        print(f"  Team Skills: {len(load('team_skills'))}")
        print(f"  Stages: {len(stages)}")
        print(f"  Gacha Pools: {len(gacha_pools)}")
        print(f"  Loot Tables: {len(load('loot_tables'))}")
        return True


# ==============================================================================
# Main
# ==============================================================================

def generate_all():
    """Generate all data and return as dict of filename -> data."""

    # Assemble all monsters
    all_monsters = []
    all_monsters.extend(EXISTING_MONSTERS)
    all_monsters.extend(GREEK_GODS_BASE)
    all_monsters.extend(NORSE_GODS_BASE)
    all_monsters.extend(EGYPTIAN_GODS_BASE)
    all_monsters.extend(CHINESE_GODS_BASE)
    all_monsters.extend(CHINESE_BEASTS_BASE)
    all_monsters.extend(GREEK_GODS_EVOLVED)
    all_monsters.extend(NORSE_GODS_EVOLVED)
    all_monsters.extend(EGYPTIAN_GODS_EVOLVED)
    all_monsters.extend(CHINESE_GODS_EVOLVED)
    all_monsters.extend(CHINESE_BEASTS_EVOLVED)
    all_monsters.extend(EVOLUTION_MATERIALS)
    all_monsters.extend(FARMABLE_FODDER)
    all_monsters.extend(SPECIAL_MONSTERS)

    # Sort by ID
    all_monsters.sort(key=lambda m: m["id"])
    all_monster_ids = {m["id"] for m in all_monsters}

    active_skills = generate_active_skills()
    leader_skills = generate_leader_skills()
    team_skills = generate_team_skills()
    stages = generate_stages()
    gacha_pools = generate_gacha_pools(all_monster_ids)
    loot_tables = generate_loot_tables(stages)
    monster_exchange = generate_monster_exchange(all_monsters)
    event_shops = generate_event_shops(all_monsters)
    player_levels = generate_player_levels()

    return {
        "monsters.json": all_monsters,
        "skills.json": active_skills,
        "leader_skills.json": leader_skills,
        "team_skills.json": team_skills,
        "stages.json": stages,
        "gacha_pools.json": gacha_pools,
        "loot_tables.json": loot_tables,
        "monster_exchange.json": monster_exchange,
        "event_shops.json": event_shops,
        "player_levels.json": player_levels,
    }


def main():
    parser = argparse.ArgumentParser(description="Generate Tower of Saviors game data")
    parser.add_argument("--output", "-o", default=None,
                        help="Output directory for JSON files")
    parser.add_argument("--validate", "-v", default=None,
                        help="Validate existing data files in directory")
    args = parser.parse_args()

    if args.validate:
        ok = validate(args.validate)
        sys.exit(0 if ok else 1)

    data = generate_all()

    if args.output:
        out_dir = Path(args.output)
        out_dir.mkdir(parents=True, exist_ok=True)
        for filename, content in data.items():
            path = out_dir / filename
            with open(path, "w", encoding="utf-8") as f:
                json.dump(content, f, indent=4, ensure_ascii=False)
            print(f"  Wrote {path} ({len(content)} entries)")
    else:
        # Print summary
        for filename, content in data.items():
            print(f"  {filename}: {len(content)} entries")

    print("\nGeneration complete.")
    print(f"  Total monsters: {len(data['monsters.json'])}")
    print(f"  Total active skills: {len(data['skills.json'])}")
    print(f"  Total leader skills: {len(data['leader_skills.json'])}")
    print(f"  Total team skills: {len(data['team_skills.json'])}")
    print(f"  Total stages: {len(data['stages.json'])}")


if __name__ == "__main__":
    main()
