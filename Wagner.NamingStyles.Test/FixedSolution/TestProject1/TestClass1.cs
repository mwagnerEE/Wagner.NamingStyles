namespace TestProject1
{
    public class TestClass1
    {
        private int _testField0;
        private int _testField1;
        private string _testField2;

        public int GetTestField1()
        {
            return _testField1;
        }

        public void SetTestField2(string value)
        {
            _testField2 = value;
        }

        public int TestField0 { get { return _testField0; } }
    }
}
