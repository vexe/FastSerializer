using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vexe.Fast.Serializer;
using Vexe.Fast.Serialization;

namespace FastSerializerTests
{
    public class Person
    {
        public string name;
        public Cat cat;
    }

    public class Cat
    {
        public string name;
        public Person owner;
    }

    public class Dummy
    {
        public int _int;
    }

    public class OnlyIgnoreDontSerialize : ISerializationPredicates
    {
        public bool IsSerializableField(FieldInfo field) { return !field.IsDefined(typeof(DontSerializeAttribute)); }
        public bool IsSerializableProperty(PropertyInfo property) { return !property.IsDefined(typeof(DontSerializeAttribute)); }
        public bool IsSerializableType(Type type) { return true; }
    }

    public class SerializeStaticOnce
    {
        static int _value;

        [DontSerialize] public static int getTimes;
        [DontSerialize] public static int setTimes;

        [Serialize]
        public static int Value
        {
            get
            {
                getTimes++;
                return _value;
            }
            set
            {
                setTimes++;
                _value = value;
            }
        }
    }

    [TestClass]
    public class FastSerializerTests
    {
        private FastSerializer serializer;
        private List<Type> types;

        [TestInitialize]
        public void InitAll()
        {
            InitTypes();
            CompileDynamic();
            //BindToTestSerializer(typeof(TestSerializer));
        }

        public void CompileWithPredicates(ISerializationPredicates p)
        {
            FastSerializer.CompileDynamic(types, null, p, onTypeGenerated: null);
        }

        private void CompileDynamic()
        {
            serializer = FastSerializer.CompileDynamic(types, null, null, null);
        }

        [TestMethod]
        public void CompileAsm()
        {
            FastSerializer.CompileAssembly(types, null, null, "SerTest", null, "TestSerializer");
        }

        public void InitTypes()
        {
            //types = new List<Type>()
            //{
            //    typeof(string[]), typeof(List<string>),
            //    typeof(Cat), typeof(Person),
            //    typeof(Dummy[]), typeof(SerializeStaticOnce[]),
            //    typeof(HashSet<int>),
            //};
        }

        [TestMethod]
        public void HashSet()
        {
            var set = new HashSet<int>() { 1, 2, 3 };
            Run(set, copy =>
            {
                CollectionAssert.AreEqual(set.ToList(), copy.ToList());
            });
        }

        [TestMethod]
        public void ArrayOfStrings()
        {
            var array = new string[] { "a", "b", "a", "d", "e", "f" };
            Run(array, copy =>
            {
                CollectionAssert.AreEqual(array, copy);
                Assert.AreSame(copy[0], copy[2]);
            });
            Run(array, copy =>
            {
                CollectionAssert.AreEqual(array, copy);
                Assert.AreSame(copy[0], copy[2]);
            });
        }

        [TestMethod]
        public void UnknownType()
        {
            var array = new int[] { 23, 45, 25, 75, 100 };
            Run(array, copy =>
            {
                CollectionAssert.AreEqual(array, copy);
            });
        }

        [TestMethod]
        public void ListOfStrings()
        {
            var list = new List<string> { "one", "two", "one", "jon", "fast", "alex" };
            Run(list, copy =>
            {
                CollectionAssert.AreEqual(list, copy);
                Assert.AreSame(copy[0], copy[2]);
            });
        }

        [TestMethod]
        public void Cycle()
        {
            var person = new Person() { name = "Ali" };
            var cat = new Cat() { name = "Sasha" };
            person.cat = cat;
            cat.owner = person;

            Run(cat, copy =>
            {
                Assert.AreEqual(copy.owner.cat, copy);
            });
        }

        [TestMethod]
        public void SerializeByRef()
        {
            var dummy1 = new Dummy() { _int = 10 };
            var dummy2 = dummy1;

            var array = new Dummy[] { dummy1, dummy2 };
            Run(array, copy => Assert.AreSame(copy[0], copy[1]));
        }

        [TestMethod]
        public void SerializeStaticOnlyOnce()
        {
            CompileWithPredicates(new OnlyIgnoreDontSerialize());
            var array = new SerializeStaticOnce[] { new SerializeStaticOnce(), new SerializeStaticOnce(), new SerializeStaticOnce() };
            Run(array, copy => Assert.IsTrue(SerializeStaticOnce.getTimes == 1 && SerializeStaticOnce.setTimes == 1));
        }

        void Run<T>(T value, Action<T> assert)
        {
            using (var ms = new MemoryStream())
            {
                serializer.Serialize(ms, value);
                ms.Position = 0;
                var copy = serializer.Deserialize<T>(ms);
                assert(copy);
            }
        }
    }
}
