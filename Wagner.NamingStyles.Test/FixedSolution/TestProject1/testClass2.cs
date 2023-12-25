namespace TestProject1
{
    public class TestClass2
    {
        private int _testField0;
        private TestClass1 _testField1;
        private string _testField2;

        public TestClass2(TestClass1 testClass1)
        {
            _testField1 = testClass1;
            _testField0 = testClass1.TestField0;
        }


        public void SetTestField2(string value)
        {
            _testField2 = value;
        }

        public int TestField0 { get { return _testField0; } }
    }
}
