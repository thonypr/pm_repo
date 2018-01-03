using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Configuration;

namespace productMadness
{
    class Program
    {
        public static List<TestRail.Types.User> GetUsers(TestRail.TestRailClient client)
        {
            List<TestRail.Types.User> users = new List<TestRail.Types.User>();
            try
            {
                users = client.GetUsers();
            }
            catch(Exception e)
            {
                logInFile(String.Format("{0}::Error in getting users", DateTime.Now));
            }
            return users;
        }

        public static String GetUserById(ulong id, List<TestRail.Types.User> users)
        {
            String userName = String.Empty;
            try
            {
                userName = users.Where(u => u.ID == id).ToList()[0].Name;
            }
            catch(Exception e)
            {
                logInFile(String.Format("{0}::Error when getting username for user {1}", DateTime.Now, id));
            }
            return userName;
        }

        public static void createIfNoFile(string pathToFile)
        {
            if (File.Exists(pathToFile))
            {
                //ok, do nothing
            }
            else
            {
                //no file, need to create one
                using (File.Create(pathToFile)) { }
            }
        }

        public static void logInFile(String log)
        {
            String fileNamePostfix = DateTime.Now.Day.ToString() + DateTime.Now.Month.ToString();
            String logFile = String.Empty;
            var path = ConfigurationManager.AppSettings["defectivesExportPath"].ToString();
            var dir = path + String.Format("//defs{0}.txt", fileNamePostfix);
            createIfNoFile(dir);
            try
            {
                using (StreamWriter sw = new StreamWriter(dir, true))
                {
                    sw.WriteLine(log);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }
        }

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

        public static TimeSpan GetTimeSpanFromString(String estimate)
        {
            TimeSpan ts = new TimeSpan();
            try
            {
                String test = "1h 20m 5s";

            }
            catch(Exception e)
            {
                //logInFile(String.Format("{0}::Error when trying to get timespan", DateTime.Now));
            }
            return ts;
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
            var projects = testrail.GetProjects().Where(x => x.Name == "PM - Back Office & Server" ||
                                                        x.Name == "PM - Cashman Casino - Mobile" ||
                                                        x.Name == "PM - FaFaFa Gold - Mobile" ||
                                                        x.Name == "PM - Heart of Vegas - Mobile" ||
                                                        x.Name == "PM - Heart of Vegas - Web" ||
                                                        x.Name == "PM - Cashman Casino - Web" ||
                                                        x.Name == "PM - Lightning Link - Mobile");

            //TimeSpan ts = new TimeSpan(1, 2, 3);
            //var xt = ts.TotalSeconds;
            //Console.WriteLine(String.Format("Period--Project--Milestone--TestCase--TestCaseId--TestResultId--TestedBy--Elapsed--Estimate"));
            var users = GetUsers(testrail);
            //var xu = GetUserById(users[0].ID, users);
            //var t = 0;
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
                                var last_result = results_group.OrderByDescending(r => r.CreatedOn).ToList()[0];
                                var test_info = testrail.GetTest(last_result.TestID);
                                TestRail.Types.Case case_info = new TestRail.Types.Case();
                                try
                                {
                                    case_info = testrail.GetCase(test_info.CaseID.Value);
                                    var case_estimate = case_info.Estimate;
                                    var createdByName = GetUserById(last_result.CreatedBy.Value, users);

                                    //Console.WriteLine(String.Format("{0}--{1}--{2}--{3}--{4}--{5}--{6}--{7}--{8}::{9}::{10}", last_result.TestID, project.Name, pm.Name, test_info.Title, case_info.ID, last_result.ID, last_result.CreatedBy, last_result.Elapsed, case_estimate, plan.ID, rp.ID));
                                    pm_repo.Model.TestResultEntry entry = new pm_repo.Model.TestResultEntry(project.Name, project.ID, pm.Name, pm.ID, test_info.Title, test_info.ID.Value,
                                        case_info.ID.Value, last_result.ID, createdByName, last_result.Elapsed.Value.TotalSeconds, 2);//elapsedInMin, estimateInMin);
                                    var et = 0;
                                }
                                catch(Exception e)
                                {
                                    //Console.WriteLine(String.Format("Error when trying to export\nTRID:{0}\nTest:{1}", last_result, results_group.ToList()[0].TestID));
                                    // write to file creds for defected case
                                    logInFile(String.Format("Project:{0}::Milestone:{1}::Plan:{2}::Run:{3}::Test:{4}::TestResult:{5}", project.ID, pm.ID, plan.ID, rp.ID, last_result.TestID, last_result.ID));
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
