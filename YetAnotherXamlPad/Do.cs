using System;

namespace YetAnotherXamlPad
{
    internal static class Do
    {
        public static void Nothing<T1, T2>(T1 t, T2 u)
        {
        }

        public static (Func<Func<Exception, TResult>, TResult> Catch, byte __unsued) Try<TResult>(Func<TResult> func)
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

        public static (Action<Action<Exception>> Catch, byte __unsued) Try(Action action)
        {
            var @try = Try(() => { action(); return 0; });
            var Catch = new Action<Action<Exception>>(@catch =>
            {
                @try.Catch(exception => { @catch(exception); return 0; });
            });

            return (Catch, default);
        }

        public static (Func<Func<Exception, EitherRightFactory<Exception>>, Either<TResult, Exception>> Catch, byte __unsued) Try<TResult>(
            Func<EitherLeftFactory<TResult>> func)
        {
            var @try = Try(() => Either.Left<TResult, Exception>(func().Left));
            var Catch = new Func<Func<Exception, EitherRightFactory<Exception>>, Either<TResult, Exception>>(@catch =>
                @try.Catch(exception => Either.Right(@catch(exception).Right)));
            return (Catch, default);
        }
    }
}
