﻿using CodeKicker.BBCode;
using core.Modules.Class;
using core.Modules.Problem;
using core.Modules.ProblemSet;
using core.Modules.User;
using redinc_reboot.Filters;
using redinc_reboot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebMatrix.WebData;

namespace redinc_reboot.Controllers
{
    [InitializeSimpleMembership]
    public class ProblemController : Controller
    {
        [Authorize]
        public ActionResult Edit(int id = 0)
        {
            try
            {
                ProblemViewModel model = new ProblemViewModel();
                //If the id is 0, we're creating a new problem
                if (id == 0)
                {
                    model.Problem = new ProblemData();
                    model.Problem.Class = new ClassData((int) Session["ClassId"]);
                }
                //Otherwise we're editing an existing problem
                else
                {
                    model.Problem = GlobalStaticVars.StaticCore.GetProblemById(id);
                    model.Sets = GlobalStaticVars.StaticCore.GetSetsForProblem(id);
                }
                ViewBag.IsInstructor = ((UserType) Session["UserType"]) == UserType.Instructor;
                return View(model);
            }
            catch (Exception e)
            {
                return RedirectToAction("ServerError", "Error");
            }
        }

        [Authorize]
        [HttpPost]
        public ActionResult Edit(ProblemViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    //If the id is 0, we're creating a new problem
                    if (model.Problem.Id == 0)
                        model.Problem.Id = GlobalStaticVars.StaticCore.AddProblem(model.Problem);
                    //Otherwise we're editing an existing problem
                    else
                        GlobalStaticVars.StaticCore.ModifyProblem(model.Problem);

                    GlobalStaticVars.StaticCore.UpdateProblemSets(model.Problem.Id, model.Sets);

                    //This is necessary in case bad prereqs (ex. duplicates) are removed by the backend
                    model.Sets = GlobalStaticVars.StaticCore.GetSetsForProblem(model.Problem.Id);
                }
                ViewBag.IsInstructor = ((UserType)Session["UserType"]) == UserType.Instructor;
                ModelState.Clear();
                return View(model);
            }
            catch (Exception e)
            {
                return RedirectToAction("ServerError", "Error");
            }
        }

        public ActionResult ReadOnly(int id)
        {
            try
            {
                ProblemData prob = GlobalStaticVars.StaticCore.GetProblemById(id);
                prob.Description = BBCode.ToHtml(prob.Description ?? "");
                prob.SolutionCode = null; //Do not send solution code to client so it can't be seen and used to cheat the problem

                return View(prob);
            }
            catch (Exception e)
            {
                return RedirectToAction("ServerError", "Error");
            }
        }

        [Authorize]
        public ActionResult Solve(int id)
        {
            try
            {
                List<ProblemData> problems = GlobalStaticVars.StaticCore.GetUnsolvedProblemsForSet(id, WebSecurity.CurrentUserId);
                //Randomly choose a problem for the user to solve
                ProblemData prob = problems[new Random().Next(problems.Count)];
                prob.Description = BBCode.ToHtml(prob.Description ?? "");
                prob.SolutionCode = null; //Do not send solution code to client so it can't be seen and used to cheat the problem

                return View(prob);
            }
            catch (Exception e)
            {
                return RedirectToAction("ServerError", "Error");
            }
        }

        [Authorize]
        [HttpPost]
        public JsonResult Solve(string code, int id)
        {
            try
            {
                ProblemData prob = GlobalStaticVars.StaticCore.GetProblemById(id);
                string output = GlobalStaticVars.StaticCore.SolveProblem(WebSecurity.CurrentUserId, prob, code);
                if (output == null)
                    return Json(new {success = true});
                else
                    return Json(new {success = false, output = output});
            }
            catch (Exception e)
            {
                RedirectToAction("ServerError", "Error");
                return new JsonResult();
            }
        }

        [Authorize]
        [HttpPost]
        public JsonResult Test(int id, string instructorCode, string studentCode)
        {
            try
            {
                ProblemData prob = new ProblemData(id);
                prob.SolutionCode = instructorCode;
                string output = GlobalStaticVars.StaticCore.SolveProblem(WebSecurity.CurrentUserId, prob, studentCode, false);
                if (output == null)
                    return Json(new { success = true });
                else
                    return Json(new { success = false, output = output });
            }
            catch (Exception e)
            {
                RedirectToAction("ServerError", "Error");
                return new JsonResult();
            }
        }

        [Authorize]
        public ActionResult AddProblemSet(ProblemSetData model)
        {
            try
            {
                //Return the partial view to for a problem set table row
                return PartialView("EditorTemplates/ProblemSetRow", model);
            }
            catch (Exception e)
            {
                return RedirectToAction("ServerError", "Error");
            }
        }

        [Authorize]
        [HttpPost]
        public ActionResult Delete(int id)
        {
            try
            {
                GlobalStaticVars.StaticCore.DeleteProblem(id);

                return RedirectToAction("Home", "Class", new {id = Session["ClassId"]});
            }
            catch (Exception e)
            {
                return RedirectToAction("ServerError", "Error");
            }
        }
    }
}
