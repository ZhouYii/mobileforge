using System;
using NUnit.Framework;
using MobileForge.Domain;

namespace MobileForge.Tests.Domain
{
    [TestFixture]
    public class BigNumberTests
    {
        [Test]
        public void FromDouble_Zero_ReturnsZero()
        {
            var bn = BigNumber.FromDouble(0);
            Assert.AreEqual(0, bn.Mantissa);
            Assert.AreEqual(0, bn.Exponent);
        }

        [Test]
        public void FromDouble_SmallNumber_Normalizes()
        {
            var bn = BigNumber.FromDouble(123);
            Assert.AreEqual(1.23, bn.Mantissa, 0.001);
            Assert.AreEqual(2, bn.Exponent);
        }

        [Test]
        public void FromDouble_LargeNumber_Normalizes()
        {
            var bn = BigNumber.FromDouble(1234567);
            Assert.AreEqual(1.234567, bn.Mantissa, 0.0001);
            Assert.AreEqual(6, bn.Exponent);
        }

        [Test]
        public void ToDouble_ReturnsOriginal()
        {
            var bn = BigNumber.FromDouble(12345);
            Assert.AreEqual(12345, bn.ToDouble(), 0.01);
        }

        [Test]
        public void Add_SameExponent_AddsMantissas()
        {
            var a = new BigNumber(1.0, 3);
            var b = new BigNumber(2.0, 3);
            var result = a.Add(b);
            Assert.AreEqual(3.0, result.Mantissa, 0.001);
            Assert.AreEqual(3, result.Exponent);
        }

        [Test]
        public void Add_DifferentExponents_Normalizes()
        {
            var a = new BigNumber(1.0, 6);
            var b = new BigNumber(1.0, 3);
            var result = a.Add(b);
            Assert.AreEqual(1.001, result.Mantissa, 0.0001);
            Assert.AreEqual(6, result.Exponent);
        }

        [Test]
        public void Add_FirstMuchLarger_ReturnsFirst()
        {
            var a = new BigNumber(1.0, 20);
            var b = new BigNumber(1.0, 3);
            var result = a.Add(b);
            Assert.AreEqual(1.0, result.Mantissa, 0.001);
            Assert.AreEqual(20, result.Exponent);
        }

        [Test]
        public void Add_Zero_ReturnsOther()
        {
            var a = new BigNumber(0, 0);
            var b = new BigNumber(5.0, 3);
            Assert.AreEqual(b.Mantissa, a.Add(b).Mantissa, 0.001);
            Assert.AreEqual(b.Exponent, a.Add(b).Exponent);
        }

        [Test]
        public void Multiply_AddsExponents()
        {
            var a = new BigNumber(2.0, 3);
            var b = new BigNumber(3.0, 4);
            var result = a.Multiply(b);
            Assert.AreEqual(6.0, result.Mantissa, 0.001);
            Assert.AreEqual(7, result.Exponent);
        }

        [Test]
        public void MultiplyScalar_ScalesMantissa()
        {
            var a = new BigNumber(2.0, 3);
            var result = a.MultiplyScalar(5);
            Assert.AreEqual(10.0, result.Mantissa, 0.001);
            Assert.AreEqual(3, result.Exponent);
        }

        [Test]
        public void CompareTo_SameExponent_ComparesMantissa()
        {
            var a = new BigNumber(2.0, 3);
            var b = new BigNumber(5.0, 3);
            Assert.AreEqual(-1, a.CompareTo(b));
            Assert.AreEqual(1, b.CompareTo(a));
            Assert.AreEqual(0, a.CompareTo(a));
        }

        [Test]
        public void CompareTo_DifferentExponent_ComparesExponent()
        {
            var a = new BigNumber(9.0, 2);
            var b = new BigNumber(1.0, 3);
            Assert.AreEqual(-1, a.CompareTo(b));
            Assert.AreEqual(1, b.CompareTo(a));
        }

        [Test]
        public void Format_Zero_ReturnsZero()
        {
            var bn = new BigNumber(0, 0);
            Assert.AreEqual("0", bn.Format());
        }

        [Test]
        public void Format_SmallNumber_NoSuffix()
        {
            var bn = BigNumber.FromDouble(123);
            Assert.AreEqual("123.00", bn.Format());
        }

        [Test]
        public void Format_Thousands_KSuffix()
        {
            var bn = BigNumber.FromDouble(1234);
            Assert.AreEqual("1.23K", bn.Format());
        }

        [Test]
        public void Format_Millions_MSuffix()
        {
            var bn = BigNumber.FromDouble(1234567);
            Assert.AreEqual("1.23M", bn.Format());
        }

        [Test]
        public void Format_Billions_BSuffix()
        {
            var bn = BigNumber.FromDouble(1234567890);
            Assert.AreEqual("1.23B", bn.Format());
        }

        [Test]
        public void Format_Trillions_TSuffix()
        {
            var bn = BigNumber.FromDouble(1234567890123.0);
            Assert.AreEqual("1.23T", bn.Format());
        }

        [Test]
        public void Format_VeryLarge_UsesScientific()
        {
            var bn = new BigNumber(1.23, 50);
            Assert.IsTrue(bn.Format().Contains("e50"));
        }

        [Test]
        public void Format_NegativeNumber_Works()
        {
            var bn = BigNumber.FromDouble(-1234);
            Assert.AreEqual("-1.23K", bn.Format());
        }

        [Test]
        public void Format_CustomDecimals()
        {
            var bn = BigNumber.FromDouble(1234);
            Assert.AreEqual("1.2K", bn.Format(1));
            Assert.AreEqual("1.234K", bn.Format(3));
        }

        [Test]
        public void Normalize_AdjustsMantissa()
        {
            var bn = new BigNumber(100.0, 0);
            Assert.AreEqual(1.0, bn.Mantissa, 0.001);
            Assert.AreEqual(2, bn.Exponent);
        }

        [Test]
        public void Constructor_Normalizes()
        {
            var bn = new BigNumber(0.001, 5);
            Assert.AreEqual(1.0, bn.Mantissa, 0.001);
            Assert.AreEqual(2, bn.Exponent);
        }
    }
}
