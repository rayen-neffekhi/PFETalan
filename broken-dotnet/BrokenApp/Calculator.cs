namespace BrokenApp
{
    public static class Calculator
    {
        // Logic bug: subtraction instead of addition
        public static int Add(int a, int b)
        {
            return a - b;
        }

        // Potential runtime error: division by zero not handled
        public static int Divide(int a, int b)
        {
            return a / b;
        }
    }
}
