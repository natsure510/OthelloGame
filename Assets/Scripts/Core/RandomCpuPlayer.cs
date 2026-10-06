using System;
using System.Collections.Generic;

namespace Othello
{
    /// <summary>Selects one of the supplied legal moves without changing the board or turn.</summary>
    public sealed class RandomCpuPlayer
    {
        private readonly Random random;

        public RandomCpuPlayer() : this(new Random())
        {
        }

        public RandomCpuPlayer(Random random)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>Returns false when there are no legal moves. Only use the output when this returns true.</summary>
        public bool TryChooseMove(IReadOnlyList<BoardPosition> legalMoves, out BoardPosition position)
        {
            if (legalMoves == null)
            {
                throw new ArgumentNullException(nameof(legalMoves));
            }

            position = default;
            if (legalMoves.Count == 0)
            {
                return false;
            }

            position = legalMoves[random.Next(legalMoves.Count)];
            return true;
        }
    }
}
