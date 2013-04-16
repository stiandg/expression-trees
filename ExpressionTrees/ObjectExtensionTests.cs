using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ExpressionTrees
{
    public class ObjectExtensionTests
    {
        [Test]
        public void TryFetch_WithStringProperty_ReturnsValueAsString()
        {
            const string expected = Any.String;
            var obj = new TestClass0 {Class1 = new TestClass1 {StringProperty = expected}};

            var result = obj.TryFetch(o => o.Class1.StringProperty);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void TryFetch_WithPropertyNull_ReturnsEmptyString()
        {
            var obj = new TestClass1 {Class0 = null};

            var result = obj.TryFetch(o => o.Class0.StringProperty);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void ToString_WithPropertyNullAndEndPropertyValueType_ReturnsEmptyString()
        {
            var obj = new TestClass1 {Class0 = null};

            var result = obj.TryFetch(o => o.Class0.IntProperty).ToString();

            Assert.That(result, Is.EqualTo("0"));
        }

        [Test]
        public void TryFetch_WithPropertyNullAndFallbackText_ReturnsFallbackText()
        {
            var obj = new TestClass1 {Class0 = null};

            var result = obj.TryFetch(o => o.Class0.IntProperty).ToString(Any.String);

            Assert.That(result, Is.EqualTo(Any.String));
        }

        [Test]
        public void TryFetch_WithObjectGraph_ReturnsValueAsString()
        {
            var obj = new TestClass0
                {
                    Class1 =
                        new TestClass1
                            {
                                Class0 = new TestClass0 {Class1 = new TestClass1 {StringProperty = Any.OtherString}}
                            }
                };

            string result = obj.TryFetch(o => o.Class1.Class0.Class1.StringProperty);

            Assert.That(result, Is.EqualTo(Any.OtherString));
        }

        [Test]
        public void TryFetch_WithMethodCall_ReturnsValue()
        {
            var obj = new TestClass0 {StringProperty = Any.String};

            string result = obj.TryFetch(o => o.GetString());

            Assert.That(result, Is.EqualTo(Any.String));
        }

        [Test]
        public void TryFetch_WithMethodCallWithArgument_ReturnsValue()
        {
            var obj = new TestClass0();

            string result = obj.TryFetch(o => o.ConvertToString(Any.Int));

            Assert.That(result, Is.EqualTo(Any.Int.ToString()));
        }

        [Test]
        public void TryFetch_WithObjectGraphWithMethodCall_ReturnsValue()
        {
            var obj0 = new TestClass0 {StringProperty = Any.String};
            var obj1 = new TestClass1 {Class0 = obj0};

            string result = obj1.TryFetch(o => o.Class0.GetString());

            Assert.That(result, Is.EqualTo(Any.String));
        }

        [Test]
        public void TryFetch_WithExtensionMethod_ReturnsValue()
        {
            var obj = new TestClass0();

            var result = obj.TryFetch(o => o.Ints.ToList());

            Assert.That(result.Count(), Is.EqualTo(2));
        }

        [Test]
        public void TryFetch_WithMethodCallLambda_ReturnsValue()
        {
            var obj = new TestClass0();

            int result = obj.TryFetch(o => o.Ints.Single(i => i == Any.Int));

            Assert.That(result, Is.EqualTo(Any.Int));
        }

        [Test]
        public void TryFetch_WithNullInPath_ReturnsDefaultValue()
        {
            var obj = new TestClass0();

            bool result0 = obj.TryFetch(o => o.Class1.BoolProperty);
            long result1 = obj.TryFetch(o => o.Class1.LongProperty);
            string result2 = obj.TryFetch(o => o.Class1.ToString());

            Assert.That(result0, Is.False);
            Assert.That(result1, Is.EqualTo(0));
            Assert.That(result2, Is.Empty);
        }

        [Test]
        public void TryFetch_SetIsNull_ReturnsEmptySet()
        {
            var obj = new TestClass2();

            var result = obj.TryFetch(o => o.Class2.Set);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        private class TestClass0
        {
            public string StringProperty { get; set; }
            public int IntProperty { get; set; }
            public TestClass1 Class1 { get; set; }

            public string GetString()
            {
                return StringProperty;
            }

            public string ConvertToString(int argument)
            {
                return argument.ToString();
            }

            public IEnumerable<int> Ints
            {
                get { return new List<int> {Any.Int, Any.OtherInt}; }
            }
        }

        private class TestClass1
        {
            public bool BoolProperty { get; set; }
            public long LongProperty { get; set; }
            public string StringProperty { get; set; }
            public virtual TestClass0 Class0 { get; set; }
        }

        private class TestClass2
        {
            public TestClass2 Class2 { get; set; }
            public ISet<TestClass2> Set { get; set; }
        }
    }

    public class Any
    {
        public const string String = "SomeString";
        public const string OtherString = "SomeOtherString";
        public const int Int = 42;
        public const int OtherInt = 1979;
    }
}