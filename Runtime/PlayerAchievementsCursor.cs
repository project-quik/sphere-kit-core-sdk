using System;
using System.Threading.Tasks;

namespace SphereKit
{
    public class PlayerAchievementsCursor
    {
        public Func<Task<PlayerAchievement[]>> Next;

        internal PlayerAchievementsCursor(Func<Task<PlayerAchievement[]>> next)
        {
            Next = next;
        }
    }
}