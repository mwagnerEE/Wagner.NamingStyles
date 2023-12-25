namespace TestProject1
{
    public class testClass2
    {
        private int _testField0;
        private TestClass1 TestField1;
        private string testField2;

        public testClass2(TestClass1 testClass1)
        {
            TestField1 = testClass1;
            _testField0 = testClass1.testField0;
        }


        public void _setTestField2(string value)
        {
            testField2 = value;
        }

        public int testField0 { get { return _testField0; } }
    }
}
