class_name MFBigNumber extends RefCounted
## Big number system for idle games.
## Float mantissa + int exponent. Formats as "1.23M", "4.56B", etc.

var mantissa: float
var exponent: int

const SUFFIXES := ["", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc"]


func _init(m: float = 0.0, e: int = 0) -> void:
	mantissa = m
	exponent = e
	_normalize()


static func from_float(value: float) -> MFBigNumber:
	if value == 0.0:
		return MFBigNumber.new(0.0, 0)
	var e := int(floor(log(absf(value)) / log(10.0)))
	var m := value / pow(10.0, e)
	return MFBigNumber.new(m, e)


func to_float() -> float:
	return mantissa * pow(10.0, exponent)


func add(other: MFBigNumber) -> MFBigNumber:
	if other.mantissa == 0.0:
		return MFBigNumber.new(mantissa, exponent)
	if mantissa == 0.0:
		return MFBigNumber.new(other.mantissa, other.exponent)
	var diff := exponent - other.exponent
	if diff >= 15:
		return MFBigNumber.new(mantissa, exponent)
	if diff <= -15:
		return MFBigNumber.new(other.mantissa, other.exponent)
	var m: float
	var e: int
	if diff >= 0:
		m = mantissa + other.mantissa * pow(10.0, -diff)
		e = exponent
	else:
		m = mantissa * pow(10.0, diff) + other.mantissa
		e = other.exponent
	return MFBigNumber.new(m, e)


func multiply(other: MFBigNumber) -> MFBigNumber:
	return MFBigNumber.new(mantissa * other.mantissa, exponent + other.exponent)


func multiply_scalar(scalar: float) -> MFBigNumber:
	return MFBigNumber.new(mantissa * scalar, exponent)


func is_greater_than(other: MFBigNumber) -> bool:
	if exponent != other.exponent:
		return exponent > other.exponent
	return mantissa > other.mantissa


## Format for display: "1.23M", "456K", etc.
func format(decimals: int = 2) -> String:
	if mantissa == 0.0:
		return "0"
	var suffix_index := exponent / 3
	if suffix_index < 0:
		return str(snapped(to_float(), pow(10, -decimals)))
	if suffix_index >= SUFFIXES.size():
		return "%.*fe%d" % [decimals, mantissa, exponent]
	var display_mantissa := mantissa * pow(10.0, exponent % 3)
	return "%.*f%s" % [decimals, display_mantissa, SUFFIXES[suffix_index]]


func _normalize() -> void:
	if mantissa == 0.0:
		exponent = 0
		return
	while absf(mantissa) >= 10.0:
		mantissa /= 10.0
		exponent += 1
	while absf(mantissa) < 1.0 and mantissa != 0.0:
		mantissa *= 10.0
		exponent -= 1
