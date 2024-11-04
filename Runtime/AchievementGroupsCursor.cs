using System;
using System.Threading.Tasks;

namespace SphereKit
{
    public class AchievementGroupsCursor
    {
        public Func<Task<AchievementGroup[]>> Next;

        internal AchievementGroupsCursor(Func<Task<AchievementGroup[]>> next)
        {
            Next = next;
        }
    }
}