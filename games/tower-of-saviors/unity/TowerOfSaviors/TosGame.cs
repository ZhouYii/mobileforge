using System;
using System.Collections.Generic;
using MobileForge.Infrastructure;
using MobileForge.Domain;
using MobileForge.Presentation;

namespace TowerOfSaviors
{
    /// <summary>
    /// Main entry point for Tower of Saviors. Wires framework modules.
    /// Mirrors tos_game.gd — loads JSON data, creates domain modules,
    /// registers skill conditions/effects, sets up UIRouter, navigates to title.
    /// </summary>
    public class TosGame
    {
        private readonly GameData _gameData;
        private readonly PlayerState _playerState;
        private readonly EventBus _eventBus;
        private readonly Economy _economy;
        private readonly MonsterManager _monsterManager;
        private readonly SkillPipeline _skillPipeline;
        private readonly UIRouter _uiRouter;

        public UIRouter Router => _uiRouter;
        public GameData GameData => _gameData;
        public PlayerState PlayerState => _playerState;
        public EventBus EventBus => _eventBus;
        public Economy Economy => _economy;
        public MonsterManager MonsterManager => _monsterManager;
        public SkillPipeline SkillPipeline => _skillPipeline;

        public TosGame()
        {
            // Use framework singletons (Unity has no autoloads)
            _eventBus = EventBus.Instance;
            _gameData = GameData.Instance;
            _playerState = PlayerState.Instance;

            // Initialize player state sections
            _playerState.RegisterSection("currencies", new Dictionary<string, object>
            {
                { "gems", 50 },
                { "coins", 10000 },
                { "stamina", 100 }
            });
            _playerState.RegisterSection("monsters", new Dictionary<string, object>());
            _playerState.RegisterSection("teams", new Dictionary<string, object>());
            _playerState.RegisterSection("progress", new Dictionary<string, object>
            {
                { "rank", 1 },
                { "exp", 0 }
            });

            // Create domain modules
            _monsterManager = new MonsterManager(defId => GetMonsterDefData(defId));
            _economy = new Economy(_playerState, _eventBus);
            _skillPipeline = new SkillPipeline();

            // Register ToS-specific skill conditions and effects
            TosSkillRegistration.Register(_skillPipeline);

            // Setup UI router
            var registry = new ScreenRegistry();
            _uiRouter = new UIRouter(registry);
            RegisterScreens();
        }

        /// <summary>
        /// Load game data from pre-parsed JSON entries.
        /// Call this after constructing TosGame, passing data loaded from
        /// the shared JSON files (monsters.json, skills.json, etc.).
        /// </summary>
        public void LoadData(string type, List<Dictionary<string, object>> entries)
        {
            _gameData.LoadDefinitions(type, entries);
        }

        /// <summary>
        /// Begin the game by navigating to the title screen.
        /// Call after all data has been loaded.
        /// </summary>
        public void Start()
        {
            _uiRouter.Navigate("title");
        }

        private Dictionary<string, object> GetMonsterDefData(int defId)
        {
            var def = _gameData.GetDefinition("monsters", defId);
            return def?.Raw() ?? new Dictionary<string, object>();
        }

        private void RegisterScreens()
        {
            _uiRouter.Register("title", parameters =>
                new TitleScreen(_uiRouter));

            _uiRouter.Register("dungeon_select", parameters =>
            {
                var screen = new DungeonSelectScreen();
                screen.Setup(_gameData, _economy, _uiRouter);
                return screen;
            });

            _uiRouter.Register("team_select", parameters =>
            {
                var screen = new TeamSelectScreen();
                screen.Setup(_monsterManager, _playerState, _uiRouter, parameters);
                return screen;
            });

            _uiRouter.Register("battle", parameters =>
            {
                var screen = new BattleScreen();
                screen.Setup(_gameData, _monsterManager, _skillPipeline, _economy, _eventBus, _uiRouter, parameters);
                return screen;
            });

            _uiRouter.Register("result", parameters =>
            {
                var screen = new ResultScreen();
                screen.Setup(parameters);
                return screen;
            });

            _uiRouter.Register("gacha", parameters =>
            {
                var screen = new GachaScreen();
                screen.Setup(_gameData, _economy, _monsterManager, _uiRouter);
                return screen;
            });

            _uiRouter.Register("monster_box", parameters =>
            {
                var screen = new MonsterBoxScreen();
                screen.Setup(_monsterManager, _uiRouter);
                return screen;
            });

            _uiRouter.Register("shop", parameters =>
            {
                var screen = new ShopScreen();
                screen.Setup(_economy, _uiRouter);
                return screen;
            });
        }
    }
}
