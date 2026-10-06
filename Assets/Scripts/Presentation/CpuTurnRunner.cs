using System;
using System.Collections;
using UnityEngine;

namespace Othello
{
    /// <summary>Waits and applies CPU moves until the player can play or the game ends.</summary>
    public sealed class CpuTurnRunner
    {
        private readonly RandomCpuPlayer cpuPlayer;
        private readonly float moveDelay;

        public CpuTurnRunner(RandomCpuPlayer cpuPlayer, float moveDelay)
        {
            this.cpuPlayer = cpuPlayer ?? throw new ArgumentNullException(nameof(cpuPlayer));
            if (moveDelay < 0f || float.IsNaN(moveDelay) || float.IsInfinity(moveDelay))
            {
                throw new ArgumentOutOfRangeException(nameof(moveDelay));
            }

            this.moveDelay = moveDelay;
        }

        public IEnumerator Run(OthelloGame game, Action onBoardChanged)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            if (onBoardChanged == null)
            {
                throw new ArgumentNullException(nameof(onBoardChanged));
            }

            return PlayTurns(game, onBoardChanged);
        }

        private IEnumerator PlayTurns(OthelloGame game, Action onBoardChanged)
        {
            while (!game.IsGameOver && game.CurrentTurn == StoneColor.White)
            {
                // Yield even at zero delay so starting a turn never applies a move synchronously.
                if (moveDelay > 0f)
                {
                    yield return new WaitForSeconds(moveDelay);
                }
                else
                {
                    yield return null;
                }

                if (game.IsGameOver || game.CurrentTurn != StoneColor.White)
                {
                    yield break;
                }

                var legalMoves = OthelloRules.GetLegalMoves(game.Board, StoneColor.White);
                if (!cpuPlayer.TryChooseMove(legalMoves, out BoardPosition position)
                    || !game.TryPlaceStone(position, StoneColor.White))
                {
                    throw new InvalidOperationException("An active CPU turn must have a legal move.");
                }

                onBoardChanged();
            }
        }
    }
}
