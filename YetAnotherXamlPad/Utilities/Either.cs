using System;

namespace YetAnotherXamlPad.Utilities
{
    [Serializable]
    internal struct Either<TLeft, TRight>
    {
        public Either(TLeft left, TRight right, bool isLeft)
            : this()
        {
            _left = left;
            _right = right;
            _isLeft = isLeft;
        }

        public TResult Fold<TResult>(Func<TLeft, TResult> getFromLeft, Func<TRight, TResult> getFromRight) =>
            _isLeft ? getFromLeft(_left) : getFromRight(_right);

        public void Fold(Action<TLeft> processLeft, Action<TRight> processRight)
        {
            if (_isLeft)
            {
                processLeft(_left);
            }
            else
            {
                processRight(_right);
            }
        }

        public static implicit operator Either<TLeft, TRight>(EitherLeftFactory<TLeft> leftFactory) =>
            new Either<TLeft, TRight>(leftFactory.Left, default, true);

        public static implicit operator Either<TLeft, TRight>(EitherRightFactory<TRight> rightFactory) =>
            new Either<TLeft, TRight>(default, rightFactory.Right, false);

        // Immutable fields would prevent Either from being de-serialized an a different AppDomain.
        // ReSharper disable FieldCanBeMadeReadOnly.Local
        private bool _isLeft;
        private TLeft _left;
        private TRight _right;
        // ReSharper enable FieldCanBeMadeReadOnly.Local
    }

    internal static class Either
    {
        public static Either<TLeft, TRight> Left<TLeft, TRight>(TLeft left) =>
            new Either<TLeft, TRight>(left, default, true);

        public static Either<TLeft, TRight> Right<TLeft, TRight>(TRight right) =>
            new Either<TLeft, TRight>(default, right, false);

        public static EitherLeftFactory<TLeft> Left<TLeft>(TLeft left) =>
            new EitherLeftFactory<TLeft>(left);

        public static EitherRightFactory<TRight> Right<TRight>(TRight right) =>
            new EitherRightFactory<TRight>(right);

        public static Either<TLeft, TResult>? Map<TLeft, TRight, TResult>(
            this Either<TLeft, TRight>? @this, 
            Func<TRight, TResult> getFromRight) =>
            @this?.Fold(Left<TLeft, TResult>, right => Right<TLeft, TResult>(getFromRight(right)));

        public static Either<TLeft, TResult>? FlatMap<TLeft, TRight, TResult>(
            this Either<TLeft, TRight>? @this,
            Func<TRight, Either<TLeft, TResult>> getFromRight) => 
            @this?.Fold(Left<TLeft, TResult>, getFromRight);

        public static TRight GetOrElse<TLeft, TRight>(this Either<TLeft, TRight> @this, TRight or = default) =>
            @this.Fold(_ => or, right => right);
    }

    internal readonly struct EitherLeftFactory<TLeft>
    {
        public EitherLeftFactory(TLeft left)
            : this()
        {
            Left = left;
        }

        public readonly TLeft Left;
    }

    internal readonly struct EitherRightFactory<TRight>
    {
        public EitherRightFactory(TRight right)
            : this()
        {
            Right = right;
        }

        public readonly TRight Right;
    }
}
