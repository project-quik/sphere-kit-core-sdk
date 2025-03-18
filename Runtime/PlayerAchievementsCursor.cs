using System;
using System.Threading.Tasks;

namespace SphereKit
{
    public class PlayerAchievementsCursor
    {
        public Func<Task<PlayerAchievement[]>> Next { get; internal set; }
        public bool HasNext { get; internal set; } = true;

        internal PlayerAchievementsCursor()
        {
        }
    }
}