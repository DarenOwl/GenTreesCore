using System.Linq;
using GenTreesCore.Entities;
using Microsoft.AspNetCore.Mvc;
using GenTreesCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using GenTreesCore.Services;
using System;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace GenTreesCore.Controllers
{
    [ApiController]
    [Route("{controller}/{action}")]
    public class TreesController : Controller
    {
        private TreesService treesService;
        private ModelEntityConverter modelEntityConverter;

        public TreesController(ApplicationContext context)
        {
            treesService = new TreesService(context);
            modelEntityConverter = new ModelEntityConverter(context);
        }

        /*
        public async System.Threading.Tasks.Task<IActionResult> AddTestDateTimeSetting()
        {
            
            var dateTimeSetting = Services.DbProvider.GetTestDateTimeSetting();
            dateTimeSetting.Owner = db.Users.FirstOrDefault(u => u.Login == "admin");
            db.GenTreeDateTimeSettings.Add(dateTimeSetting);
            await db.SaveChangesAsync();
            return Ok();
        }

        public async System.Threading.Tasks.Task<IActionResult> AddTestGenTree()
        {
            var tree = GenTreesCore.Services.DbProvider.GetSimpleTestGenTree();
            tree.Owner = db.Users.FirstOrDefault(u => u.Login == "admin");
            var dateTimeSetting = db.GenTreeDateTimeSettings.FirstOrDefault();
            tree.GenTreeDateTimeSetting = dateTimeSetting;
            db.GenTrees.Add(tree);
            await db.SaveChangesAsync();
            return RedirectToAction("Public", "Trees");
        }

        public async System.Threading.Tasks.Task<IActionResult> AddTestDate()
        {
            var person = db.GenTrees.Include(t => t.Persons).FirstOrDefault().Persons.FirstOrDefault();
            var era = db.GenTreeDateTimeSettings.Include(d => d.Eras).FirstOrDefault().Eras.FirstOrDefault();
            person.BirthDate = new GenTreeDateTime
            {
                Era = era,
                Year = 1874,
                Month = 2,
                Day = 20
            };
            await db.SaveChangesAsync();
            return RedirectToAction("Public", "Trees");
        }
        */

        [HttpGet]
        public JsonResult Public()
        {
            var trees = treesService.GetPublicTrees()
                .Select(tree => new GenTreeSimpleViewModel
                {
                    Id = tree.Id,
                    Name = tree.Name,
                    Description = tree.Description,
                    Creator = tree.Owner.Login,
                    LastUpdated = tree.LastUpdated.ToString("d/MM/yyyy"),
                    Image = tree.Image
                })
                .ToList();

            return Json(trees);
        }

        [Authorize]
        [HttpGet]
        public JsonResult My()
        {
            //получаем id авторизованного пользователя
            var authorizedUserId = int.Parse(HttpContext.User.Identity.Name);
            //получаем список всех его деревьев
            var trees = treesService.GetUserGenTrees(authorizedUserId)
                .Select(tree => new MyTreeSimpleViewModel
                {
                    Id = tree.Id,
                    Name = tree.Name,
                    Description = tree.Description,
                    DateCreated = tree.DateCreated.ToString("d/MM/yyyy"),
                    LastUpdated = tree.LastUpdated.ToString("d/MM/yyyy"),
                    Image = tree.Image
                })
                .ToList();

            return Json(trees);
        }

        [HttpGet]
        public IActionResult GenTree(int id)
        {
            int? authorizedUserId = null;
            if (HttpContext.User.Identity.IsAuthenticated)
                authorizedUserId = int.Parse(HttpContext.User.Identity.Name);

            var tree = treesService.GetGenTree(id);

            if (tree == null)
                return BadRequest($"no tree with id {id} found");
            if (tree.IsPrivate && tree.Owner.Id != authorizedUserId)
                return BadRequest("access denied");

            var treeModel = modelEntityConverter.ToViewModel(tree);
            treeModel.CanEdit = tree.Owner.Id == authorizedUserId;

            return Ok(JsonConvert.SerializeObject(treeModel));
        }

        [HttpPost]
        public IActionResult UpdateTree(GenTreeViewModel model)
        {
            var tree = treesService.GetGenTree(model.Id);
            if (tree == null)
                return BadRequest($"no tree with id {model.Id} found");

            try
            {
                modelEntityConverter.UpdateEntity(model, tree);
            }
            catch (Exception e)
            {
                return BadRequest($"Some server error occured: {e.Message}");
            }
            treesService.SaveChanges();
            return Ok("tree updated");
        }

        [HttpPost]
        public IActionResult Update(CustomPersonDescriptionTemplate model)
        {
            treesService.Update(model, model.Id);
            return Ok();
        }

        private string ShortenDescription(string description, int length)
        {
            if (description == null)
                return null;

            return description.Substring(0, Math.Min(length, description.Length));
        }
    }
}