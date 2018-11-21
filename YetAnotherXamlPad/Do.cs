using System;

namespace YetAnotherXamlPad
{
    internal static class Do
    {
        public static void Nothing<T1, T2>(T1 t, T2 u)
        {
        }

        public static (Func<Func<Exception, TResult>, TResult> Catch, Unit) Try<TResult>(Func<TResult> func)
        {
            var Catch = new Func<Func<Exception, TResult>, TResult>(@catch =>
            {
                try
                {
                    return func();
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
                    return @catch(e);
                }
            });

            return (Catch, default);
        }

        public static (Action<Action<Exception>> Catch, Unit) Try(Action action)
        {
            var @try = Try(() => { action(); return 0; });
            var Catch = new Action<Action<Exception>>(@catch =>
            {
                @try.Catch(exception => { @catch(exception); return 0; });
            });
            return (Catch, default);
        }
    }
}
