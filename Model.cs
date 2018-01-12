using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pm_repo
{
    class Model
    {
        public class TestResultEntry
        {
            public String projectName { get; set; }
            public ulong projectId { get; set; }
            public String milestone { get; set; }
            public ulong milestoneId { get; set; }
            public String testName { get; set; }
            public String testId { get; set; }
            public ulong caseId { get; set; }
            public ulong testResultId { get; set; }
            public String createdBy { get; set; }
            public double elapsedInSec { get; set; }
            public double estimateInSec { get; set; }
            public String period { get; set; }
            //public ulong status { get; set; }

            public TestResultEntry(String projectName, ulong projectId, String milestone, ulong milestoneId, String testName, String testId, 
                ulong caseId, ulong testResultId, String createdBy, double elapsedInSec, double estimateInSec/*, ulong status*/, String period) {

                this.projectName = projectName;
                this.projectId = projectId;
                this.milestone = milestone;
                this.milestoneId = milestoneId;
                this.testName = testName;
                this.testId = testId;
                this.caseId = caseId;
                this.testResultId = testResultId;
                this.createdBy = createdBy;
                this.elapsedInSec = elapsedInSec;
                this.estimateInSec = estimateInSec;
                //this.status = status;
                this.period = period;
            }
        }
    }
}
