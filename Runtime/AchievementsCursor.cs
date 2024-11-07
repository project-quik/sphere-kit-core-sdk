using System;
using System.Threading.Tasks;

#nullable enable
namespace SphereKit
{
    public class AchievementsCursor
    {
        public Func<Task<Achievement[]>> Next;

        internal AchievementsCursor(Func<Task<Achievement[]>> next)
        {
            Next = next;
        }
    }
}