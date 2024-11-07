#nullable enable
namespace SphereKit
{
    public readonly struct AuthState
    {
        public readonly bool IsSignedIn;
        public readonly Player? Player;

        public AuthState(bool isSignedIn, Player? player)
        {
            IsSignedIn = isSignedIn;
            Player = player;
        }
    }
}