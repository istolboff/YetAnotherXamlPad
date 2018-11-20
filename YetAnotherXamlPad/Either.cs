using System;

namespace YetAnotherXamlPad
{
    [Serializable]
    internal struct Either<TLeft, TRight>
    {
        public Either(TLeft left, TRight right, bool isLeft)
            : this()
        {
            _left = left;
            _right = right;
            IsLeft = isLeft;
        }

        public TResult Fold<TResult>(Func<TLeft, TResult> getFromLeft, Func<TRight, TResult> getFromRight)
        {
            return IsLeft ? getFromLeft(_left) : getFromRight(_right);
        }

        public void Fold(Action<TLeft> processLeft, Action<TRight> processRight)
        {
            if (IsLeft)
            {
                processLeft(_left);
            }
            else
            {
                processRight(_right);
            }
        }

        public bool IsLeft { get; }

        public static implicit operator Either<TLeft, TRight>(EitherLeftFactory<TLeft> leftFactory)
        {
            return new Either<TLeft, TRight>(leftFactory.Left, default, true);
        }

        public static implicit operator Either<TLeft, TRight>(EitherRightFactory<TRight> rightFactory)
        {
            return new Either<TLeft, TRight>(default, rightFactory.Right, false);
        }

        // Immutable fields would prevent Either from being de-serialized an a different AppDomain.
        // ReSharper disable FieldCanBeMadeReadOnly.Local
        private TLeft _left;
        private TRight _right;
        // ReSharper enable FieldCanBeMadeReadOnly.Local
    }

    internal static class Either
    {
        public static Either<TLeft, TRight> Left<TLeft, TRight>(TLeft left)
        {
            return new Either<TLeft, TRight>(left, default, true);
        }

        public static Either<TLeft, TRight> Right<TLeft, TRight>(TRight right)
        {
            return new Either<TLeft, TRight>(default, right, false);
        }

        public static EitherLeftFactory<TLeft> Left<TLeft>(TLeft left)
        {
            return new EitherLeftFactory<TLeft>(left);
        }

        public static EitherRightFactory<TRight> Right<TRight>(TRight right)
        {
            return new EitherRightFactory<TRight>(right);
        }
    }

    internal struct EitherLeftFactory<TLeft>
    {
        public EitherLeftFactory(TLeft left)
            : this()
        {
            Left = left;
        }

        public TLeft Left { get; }
    }

    internal struct EitherRightFactory<TRight>
    {
        public EitherRightFactory(TRight right)
            : this()
        {
            Right = right;
        }

        public TRight Right { get; }
    }
}
