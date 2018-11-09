using System;

namespace YetAnotherXamlPad
{
    internal static class Do
    {
        public static void Nothing<T1, T2>(T1 t, T2 u)
        {
        }

        public static (Action<Action<Exception>> Catch, byte __unsued) Try(Action action)
        {
            var Catch = new Action<Action<Exception>>(@catch =>
            {
                try
                {
                    action();
                }
                catch (OutOfMemoryException) { throw; }
                catch (StackOverflowException) { throw; }
                catch (NullReferenceException) { throw; }
                catch (IndexOutOfRangeException) { throw; }
                catch (DivideByZeroException) { throw; }
                catch (AccessViolationException) { throw; }
                catch (DataMisalignedException) { throw; }
                catch (Exception e)
                {
                    @catch(e);
                }
            });

            return (Catch, default);
        }
    }
}
