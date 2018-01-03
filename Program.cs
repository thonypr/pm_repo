using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace productMadness
{
    class Program
    {
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static List<ulong> GetRunsFromPlan(ulong planId, string login, string pass)
        {
            List<ulong> runs_from_plan = new List<ulong>();
            var client = new RestClient(String.Format("https://productmadness.testrail.com/index.php?%2Fapi%2Fv2%2Fget_plan%2F{0}=", planId));
            var request = new RestRequest(Method.GET);
            var hash = Base64Encode(login + ':' + pass);
            request.AddHeader("authorization", "Basic " + hash);
            request.AddHeader("content-type", "application/json");
            IRestResponse response = client.Execute(request);
            JObject json = JObject.Parse(response.Content.ToString());
            var entries = json["entries"];
            foreach (var entry in entries)
            {
                var plan_runs = entry["runs"];
                foreach (var pr in plan_runs)
                {
                    runs_from_plan.Add(ulong.Parse(pr["id"].ToString()));
                }
            }
            return runs_from_plan;
        }

        static void Main(string[] args)
        {
            #region testrail connect
            String confPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            String dir = System.IO.Path.GetDirectoryName(confPath);
            String login = "";
            String pass = "";
            // For crashes backup String crashFile = dir + "\\crashes" + "\\buResults.txt";
            dir = dir + "\\conf";

            try
            {   // Open the text file using a stream reader.
                using (StreamReader sr = new StreamReader(dir + "\\conf.txt"))
                {
                    // Read the stream to a string, and write the string to the console.
                    login = sr.ReadLine();
                    pass = sr.ReadLine();
                    Console.WriteLine("Welcome, " + login + " !");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }
            var baseUrl = "https://productmadness.testrail.com/index.php";
            //var login = "m.mikulik@a1qa.com";
            //var pass = "j80jMZhIxUqqUI5QIZ*L";
            TestRail.TestRailClient testrail = new TestRail.TestRailClient(baseUrl, login, pass);
            #endregion
            DateTime dateX = DateTime.Parse("01.12.2017 00:00:00");
            DateTime dateY = DateTime.Parse("31.12.2017 23:59:59");
            var projects = testrail.GetProjects().Where(x => x.Name == "PM - Back Office & Server" /*||*/
                                                             /*x.Name == "PM - Cashman Casino - Mobile"*/ /*||*/
                                                             //x.Name == "PM - FaFaFa Gold - Mobile" ||
                                                             //x.Name == "PM - Heart of Vegas - Mobile" ||
                                                             //x.Name == "PM - Heart of Vegas - Web" ||
                                                             //x.Name == "PM - Cashman Casino - Web (в будущем)" ||
                                                             /*x.Name == "PM - Lightning Link - Mobile"*/);

            //var xxxx = testrail.GetTest(5649866);
            Console.WriteLine(String.Format("Period--Project--Milestone--TestCase--TestCaseId--TestResultId--TestedBy--Elapsed--Estimate"));
            foreach (var project in projects)
            {
                var project_milestones = testrail.GetMilestones(project.ID).Where(cm => cm.IsCompleted == true && cm.CompletedOn >= dateX && cm.CompletedOn <= dateY);
                foreach (var pm in project_milestones)
                {
                    var plans = testrail.GetPlans(project.ID).Where(p => p.MilestoneID == pm.ID);
                    foreach (var plan in plans)
                    {
                        //var entries_runs = plan.JsonFromResponse["entries"];
                        // workaround
                        //List<int> runs_from_plan = new List<int>();
                        List<TestRail.Types.Run> runs_from_plan = new List<TestRail.Types.Run>();
                        var run_ids_from_plan = GetRunsFromPlan(plan.ID, login, pass);
                        foreach (var runId in run_ids_from_plan)
                        {
                            var run = testrail.GetRun(runId);
                            runs_from_plan.Add(run);
                        }
                        // workaround ended
                        foreach (var rp in runs_from_plan)
                        {
                            //var run_tests = testrail.GetTests(rp.ID.Value);
                            //foreach (var rt in run_tests)
                            //{
                                //var run_cases = testrail.GetCase()
                            var test_results = testrail.GetResultsForRun(rp.ID.Value).GroupBy(rp1 => rp1.TestID);
                            foreach (var results_group in test_results)
                            {
                                TestRail.Types.Result last_result = new TestRail.Types.Result();
                                try
                                {
                                    last_result = results_group.OrderByDescending(r => r.CreatedOn).ToList()[0];
                                    var case_info = testrail.GetCase(testrail.GetTest(last_result.TestID).CaseID.Value);
                                    var test_info = testrail.GetTest(last_result.TestID);
                                    var case_estimate = case_info.Estimate;

                                    Console.WriteLine(String.Format("{0}--{1}--{2}--{3}--{4}--{5}--{6}--{7}--{8}::{9}::{10}", last_result.TestID, project.Name, pm.Name, test_info.Title, case_info.ID, last_result.ID, last_result.CreatedBy, last_result.Elapsed, case_estimate, plan.ID, rp.ID));
                                    var c = 0;
                                }
                                catch(Exception e)
                                {
                                    //Console.WriteLine(String.Format("Error when trying to export\nTRID:{0}\nTest:{1}", last_result, results_group.ToList()[0].TestID));
                                }
                            }
                            var tt = 0;
                            //}
                            var zo = 0;
                        }

                        
                        var xxx = 0;
                        

                        var z = 0;
                    }
                    var xx = 0;
                }

                var x = 0;
            }

            var i = 0;
        }
    }
}
