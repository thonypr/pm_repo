﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Configuration;
using pm_repo;

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
            catch (Exception e)
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
            catch (Exception e)
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
            String fileNamePostfix = String.Format("_{0}", DateTime.Now.ToString("yyyy-MM-dd"));
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

        public static double GetTimeSpanFromString(String estimate)
        {
            double seconds = 0;
            try
            {
                //String test = "1h 20m 5s";
                var parts = estimate.Split(' ');
                foreach (var part in parts)
                {
                    char identifier = part[part.Count() - 1];
                    if (identifier == 'h')
                    {
                        try
                        {
                            double add = double.Parse(part.Substring(0, part.Count() - 1));
                            seconds += 60 * 60 * add;
                        }
                        catch (Exception e)
                        {
                            logInFile(String.Format("{0}::Error when trying to get timespan", DateTime.Now));
                        }
                    }
                    if (identifier == 'm')
                    {
                        try
                        {
                            double add = double.Parse(part.Substring(0, part.Count() - 1));
                            seconds += 60 * add;
                        }
                        catch (Exception e)
                        {
                            logInFile(String.Format("{0}::Error when trying to get timespan", DateTime.Now));
                        }
                    }
                    if (identifier == 's')
                    {
                        try
                        {
                            double add = double.Parse(part.Substring(0, part.Count() - 1));
                            seconds += add;
                        }
                        catch (Exception e)
                        {
                            logInFile(String.Format("{0}::Error when trying to get timespan", DateTime.Now));
                        }
                    }
                }

            }
            catch (Exception e)
            {
                //logInFile(String.Format("{0}::Error when trying to get timespan", DateTime.Now));
            }
            return seconds;
        }

        public static List<TestRail.Types.Milestone> GetAllSubMilestones(TestRail.TestRailClient client, ulong projectId)
        {
            List<TestRail.Types.Milestone> result = new List<TestRail.Types.Milestone>();
            //var initMilestones = client.GetMilestones(projectId);
            var initMilestones = client.GetMilestones(projectId).Select(mi => mi.ID).ToList();
            bool reachedBottom = false;
            //foreach (var init in initMilestones)
            while (initMilestones.Count > 0)
            {
                var init = initMilestones[0];
                //check if init has subMilestones
                var item = client.GetMilestone(init);
                //add to result
                result.Add(item);
                //remove from iteration list
                initMilestones.Remove(init);
                try
                {
                    var subMilestones = item.JsonFromResponse["milestones"].ToList();
                    foreach (var sub in subMilestones)
                    {
                        try
                        {
                            initMilestones.Add(ulong.Parse(sub["id"].ToString()));
                        }
                        catch (Exception ex)
                        {
                            if (ex is FormatException || ex is OverflowException || ex is ArgumentNullException)
                            {
                                logInFile(String.Format("{0}::Something went wrong when processing submilestone {2} for milestone {1}", DateTime.Now, init, sub));
                            }
                        }
                    }
                }
                catch (ArgumentNullException e)
                {
                    //ok, no submilestones
                }
            }

            return result;
        }

        public static bool CheckIfStatusIsValid(ulong statusId, List<String> statuses)
        {
            bool result = false;
            Parallel.ForEach(statuses, row =>
            {
                if (row.Split(':')[0] == statusId.ToString())
                {
                    result = true;

                }
            });
            return result;
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
            TestRail.TestRailClient testrail = new TestRail.TestRailClient(baseUrl, login, pass);
            #endregion
            var delta = int.Parse(ConfigurationManager.AppSettings["delta"]);
            DateTime now = DateTime.Now.AddMonths(delta);
            DateTime lastDate = new DateTime(now.Year, now.Month, 1);
            DateTime startDate = lastDate.AddMonths(-1);
            DateTime dateX = startDate;
            DateTime dateY = lastDate;

            Console.WriteLine(String.Format("From:{0}\nTo:{1}\n", dateX, dateY));
            List<string> selectedProjects = new List<string>();
            List<string> statuses = new List<string>();
            List<Model.TestResultEntry> entries = new List<Model.TestResultEntry>();
            #region get project names
            try
            {
                // Open the text file using a stream reader.
                var arrayFile = File.ReadAllLines(dir + "\\projects.txt");
                Parallel.ForEach(arrayFile, row =>
                {
                    selectedProjects.Add(row);
                });
            }
            catch (Exception e)
            {
                logInFile(String.Format("{0}::Error when getting project names", DateTime.Now));
                return;
            }

            #endregion
            #region get statuses
            try
            {
                // Open the text file using a stream reader.
                var arrayFile = File.ReadAllLines(dir + "\\statuses.txt");
                Parallel.ForEach(arrayFile, row =>
                {
                    statuses.Add(row);
                });
            }
            catch (Exception e)
            {
                logInFile(String.Format("{0}::Error when getting statuses", DateTime.Now));
                return;
            }
            #endregion
            //var cx = CheckIfStatusIsValid(98, statuses);

            /*var projects = testrail.GetProjects().Where(x => /*x.Name == "PM - Back Office & Server" ||
                                                        x.Name == "PM - Cashman Casino - Mobile" ||
                                                        x.Name == "PM - FaFaFa Gold - Mobile" ||
                                                        x.Name == "PM - Heart of Vegas - Mobile" ||
                                                        x.Name == "PM - Heart of Vegas - Web" ||
                                                        x.Name == "PM - Cashman Casino - Web" ||
                                                        x.Name == "PM - Lightning Link - Mobile");
                                                        */
            #region working with records
            var projects = testrail.GetProjects().Where(p => selectedProjects.Contains(p.Name));
            var users = GetUsers(testrail);

            #region iterate through results
            #region projects
            foreach (var project in projects)
            {
                Console.Write(String.Format("Project:{0} ...", project.Name));
                var rawMilestones = GetAllSubMilestones(testrail, project.ID);
                var project_milestones = rawMilestones.Where(cm => cm.IsCompleted == true &&
                                                                   cm.CompletedOn >= dateX &&
                                                                   cm.CompletedOn < dateY
                                                                   );
                //var project_milestones = testrail.GetMilestones(project.ID).Where(cm => cm.Name == "(Amazon) 3.2.0 Release Build - Full Test Pass 1 - Build 3.2.5 - UAT" && cm.IsCompleted == true && cm.CompletedOn >= dateX && cm.CompletedOn < dateY);
                #region milestone
                foreach (var pm in project_milestones)
                {
                    var plans = testrail.GetPlans(project.ID).Where(p => p.MilestoneID == pm.ID);
                    #region plans
                    foreach (var plan in plans)
                    {
                        //var entries_runs = plan.JsonFromResponse["entries"];
                        // workaround
                        List<TestRail.Types.Run> runs_from_plan = new List<TestRail.Types.Run>();
                        var run_ids_from_plan = GetRunsFromPlan(plan.ID, login, pass);
                        #region run
                        foreach (var runId in run_ids_from_plan)
                        {
                            var run = testrail.GetRun(runId);
                            runs_from_plan.Add(run);
                        }
                        // workaround ended
                        foreach (var rp in runs_from_plan)
                        {
                            var test_results = testrail.GetResultsForRun(rp.ID.Value).GroupBy(rp1 => rp1.TestID);
                            #region result_group = test
                            foreach (var results_group in test_results)
                            {
                                #region last result for test
                                var last_result = results_group.OrderByDescending(r => r.CreatedOn).ToList()[0];
                                var test_info = testrail.GetTest(last_result.TestID);
                                TestRail.Types.Case case_info = new TestRail.Types.Case();
                                try
                                {
                                    case_info = testrail.GetCase(test_info.CaseID.Value);
                                    var case_estimate = case_info.Estimate;
                                    var createdByName = GetUserById(last_result.CreatedBy.Value, users);
                                    var period = dateX.ToString("dd.MM.yyyy");
                                    period = String.Format("{0} 00:00:00", period);
                                    var status = last_result.StatusID.Value;
                                    var estimate = case_estimate.ToString();
                                    var elapsed = last_result.Elapsed.Value.TotalSeconds;
                                    #region elapsed & estimate check
                                    //E = Ef
                                    if (estimate == "0s" || estimate == "" || estimate == null)
                                    {
                                        //L = Lf
                                        if (elapsed == null || elapsed <= 2)
                                        {
                                            //E = 0; L = 0
                                            elapsed = 0;
                                            estimate = "0s";
                                        }
                                        //L = Lp
                                        else
                                        {
                                            //L = Lp ; E = Lp
                                            estimate = String.Format("{0}", elapsed.ToString());
                                        }
                                    }
                                    // E = Ep
                                    else
                                    {
                                        //L = Lf
                                        if (elapsed == null || elapsed <= 2)
                                        {
                                            //E = Ep ; L = Lp
                                            elapsed = GetTimeSpanFromString(estimate);
                                        }
                                        //L = Lp
                                        else
                                        {
                                            //E = Ep ; L = Lp
                                        }
                                    }
                                    #endregion
                                    if (CheckIfStatusIsValid(status, statuses) == true)
                                    {
                                        //status check passed
                                        pm_repo.Model.TestResultEntry entry = new pm_repo.Model.TestResultEntry(project.Name, project.ID, pm.Name, pm.ID, test_info.Title, 
                                            String.Format("https://productmadness.testrail.com/index.php?/tests/view/{0}", test_info.ID.Value), case_info.ID.Value, 
                                            last_result.ID, createdByName, elapsed, GetTimeSpanFromString(estimate)/*, status*/, period);
                                        entries.Add(entry);
                                    }
                                    else
                                    {
                                        logInFile(String.Format("Project:{0}::Milestone:{1}::Plan:{2}::Run:{3}::Test:{4}::TestResult:{5}", project.ID, pm.ID, plan.ID, rp.ID, last_result.TestID, last_result.ID));
                                    }
                                }
                                catch (Exception e)
                                {
                                    logInFile(String.Format("Project:{0}::Milestone:{1}::Plan:{2}::Run:{3}::Test:{4}::TestResult:{5}", project.ID, pm.ID, plan.ID, rp.ID, last_result.TestID, last_result.ID));
                                }
                                #endregion
                            }
                            #endregion
                        }
                        #endregion
                    }
                    #endregion

                    var raw_runs = testrail.GetRuns(project.ID).Where(r => r.IsCompleted == true &&
                                                                               r.CompletedOn >= dateX &&
                                                                               r.CompletedOn < dateY);

                    if (plans.Count() == 0 && raw_runs.Count() != 0)
                    {
                        #region if no plans and there are runs - export them too
                        // workaround
                        
                        //var raw_runs_ids = GetRunsFromPlan(plan.ID, login, pass);
                        #region run
                        //foreach (var runId in run_ids_from_plan)
                        //{
                        //    var run = testrail.GetRun(runId);
                        //    runs_from_plan.Add(run);
                        //}
                        // workaround ended
                        foreach (var rp in raw_runs)
                        {
                            var test_results = testrail.GetResultsForRun(rp.ID.Value).GroupBy(rp1 => rp1.TestID);
                            #region result_group = test
                            foreach (var results_group in test_results)
                            {
                                #region last result for test
                                var last_result = results_group.OrderByDescending(r => r.CreatedOn).ToList()[0];
                                var test_info = testrail.GetTest(last_result.TestID);
                                TestRail.Types.Case case_info = new TestRail.Types.Case();
                                try
                                {
                                    case_info = testrail.GetCase(test_info.CaseID.Value);
                                    var case_estimate = case_info.Estimate;
                                    var createdByName = GetUserById(last_result.CreatedBy.Value, users);
                                    var period = dateX.ToString("dd.MM.yyyy");
                                    period = String.Format("{0} 00:00:00", period);
                                    var status = last_result.StatusID.Value;
                                    var estimate = case_estimate.ToString();
                                    var elapsed = last_result.Elapsed.Value.TotalSeconds;
                                    #region elapsed & estimate check
                                    //E = Ef
                                    if (estimate == "0s" || estimate == "" || estimate == null)
                                    {
                                        //L = Lf
                                        if (elapsed == null || elapsed <= 2)
                                        {
                                            //E = 0; L = 0
                                            elapsed = 0;
                                            estimate = "0s";
                                        }
                                        //L = Lp
                                        else
                                        {
                                            //L = Lp ; E = Lp
                                            estimate = String.Format("{0}", elapsed.ToString());
                                        }
                                    }
                                    // E = Ep
                                    else
                                    {
                                        //L = Lf
                                        if (elapsed == null || elapsed <= 2)
                                        {
                                            //E = Ep ; L = Lp
                                            elapsed = GetTimeSpanFromString(estimate);
                                        }
                                        //L = Lp
                                        else
                                        {
                                            //E = Ep ; L = Lp
                                        }
                                    }
                                    #endregion
                                    if (CheckIfStatusIsValid(status, statuses) == true)
                                    {
                                        //status check passed
                                        pm_repo.Model.TestResultEntry entry = new pm_repo.Model.TestResultEntry(project.Name, project.ID, pm.Name, pm.ID, test_info.Title,
                                            String.Format("https://productmadness.testrail.com/index.php?/tests/view/{0}", test_info.ID.Value), case_info.ID.Value,
                                            last_result.ID, createdByName, elapsed, GetTimeSpanFromString(estimate)/*, status*/, period);
                                        entries.Add(entry);
                                    }
                                    else
                                    {
                                        logInFile(String.Format("Project:{0}::Milestone:{1}::Plan:{2}::Run:{3}::Test:{4}::TestResult:{5}", project.ID, pm.ID, -1, rp.ID, last_result.TestID, last_result.ID));
                                    }
                                }
                                catch (Exception e)
                                {
                                    logInFile(String.Format("Project:{0}::Milestone:{1}::Plan:{2}::Run:{3}::Test:{4}::TestResult:{5}", project.ID, pm.ID, -1, rp.ID, last_result.TestID, last_result.ID));
                                }
                                #endregion
                            }
                            #endregion
                        }
                        #endregion
                        #endregion
                    }

                }
                #endregion
                Console.Write("Done!\n");
            }
            #endregion
            #endregion
            #endregion

            //pm_repo.ExcelExport.ExcelExport.exportDataToExcel(pm_repo.ExcelExport.ExcelExport.entriesHeaderWithValues(entries));
            CsvExport.writeDataToFile(entries);
            logInFile(String.Format("Started at:{0}\nFinished at:{1}", now, DateTime.Now));
        }
    }
}
