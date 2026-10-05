using System;
using System.Collections.Generic;

namespace IFZ.ProductionPlanner
{
    internal static class ExperienceRules
    {
        public const float NoviceDays = 3f;
        public const float ModerateDays = 8f;
        public const float ExpertDays = 15f;
        public const int MaxJobs = 3;
        public const string ForemanPrefix = "foreman:";

        public static bool IsForemanEntry(string job) => job != null && job.StartsWith(ForemanPrefix, StringComparison.Ordinal);

        public static string ForemanJob(List<JobExperience> list)
        {
            foreach (var j in list)
            {
                if (IsForemanEntry(j.Job)) return j.Job.Substring(ForemanPrefix.Length);
            }
            return null;
        }

        public static bool MakeForeman(List<JobExperience> list, string job)
        {
            if (ForemanJob(list) != null) return false;
            foreach (var j in list)
            {
                if (j.Job != job || LevelFor(j.Days) != Level.Expert) continue;
                j.Job = ForemanPrefix + job;
                j.Days = 0f;
                return true;
            }
            return false;
        }

        public static bool StopForeman(List<JobExperience> list) => list.RemoveAll(j => IsForemanEntry(j.Job)) > 0;

        public static Level LevelFor(float days)
        {
            if (days >= ExpertDays) return Level.Expert;
            if (days >= ModerateDays) return Level.Moderate;
            if (days >= NoviceDays) return Level.Novice;
            return Level.None;
        }

        public static string AddDays(List<JobExperience> list, string job, float days, ref int orderCounter)
        {
            JobExperience entry = null;
            foreach (var j in list)
            {
                if (j.Job == job) entry = j;
            }
            if (entry == null)
            {
                list.RemoveAll(j => !j.Learned);
                entry = new JobExperience { Job = job };
                list.Add(entry);
            }
            bool wasLearned = entry.Learned;
            entry.Days = Math.Min(ExpertDays, entry.Days + days);
            if (wasLearned || !entry.Learned) return null;
            entry.LearnedOrder = ++orderCounter;
            int learned = 0;
            foreach (var j in list)
            {
                if (j.Learned) learned++;
            }
            if (learned <= MaxJobs) return null;
            JobExperience oldest = null;
            foreach (var j in list)
            {
                if (j == entry || !j.Learned) continue;
                if (oldest == null || j.LearnedOrder < oldest.LearnedOrder) oldest = j;
            }
            if (oldest == null) return null;
            list.Remove(oldest);
            return oldest.Job;
        }
    }
}
