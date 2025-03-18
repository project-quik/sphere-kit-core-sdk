using System;
using System.Threading.Tasks;

namespace SphereKit
{
    public class AchievementGroupsCursor
    {
        public Func<Task<AchievementGroup[]>> Next;
        public bool HasNext { get; internal set; } = true;

        internal AchievementGroupsCursor()
        {
        }
    }
}