using System;
using System.Threading.Tasks;

namespace SphereKit
{
    public class AchievementsCursor
    {
        public Func<Task<Achievement[]>> Next { get; internal set; }
        public bool HasNext { get; internal set; } = true;

        internal AchievementsCursor()
        {
        }
    }
}