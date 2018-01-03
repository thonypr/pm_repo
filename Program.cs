using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace productMadness
{
    class Program
    {
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
                                                             x.Name == "PM - Cashman Casino - Mobile" /*||*/
                                                             //x.Name == "PM - FaFaFa Gold - Mobile" ||
                                                             //x.Name == "PM - Heart of Vegas - Mobile" ||
                                                             //x.Name == "PM - Heart of Vegas - Web" ||
                                                             //x.Name == "PM - Cashman Casino - Web (в будущем)" ||
                                                             /*x.Name == "PM - Lightning Link - Mobile"*/);

            foreach (var project in projects)
            {
                var project_milestones = testrail.GetMilestones(project.ID).Where(cm => cm.IsCompleted == true && cm.CompletedOn >= dateX && cm.CompletedOn <= dateY);
                foreach (var pm in project_milestones)
                {
                    var plans = testrail.GetPlans(project.ID).Where(p => p.MilestoneID == pm.ID);
                    foreach (var plan in plans)
                    {
                        var entries_runs = plan.JsonFromResponse["entries"];
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
