using System.Linq;
using GenTreesCore.Entities;
using Microsoft.AspNetCore.Mvc;
using GenTreesCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using GenTreesCore.Services;
using System;
using System.Collections.Generic;

namespace GenTreesCore.Controllers
{
    [ApiController]
    [Route("{controller}/{action}")]
    public class TreesController : Controller
    {
        private ITreeRepository treeRepository;
        private TreeUpdateService updateService;
        private IDateTimeSettingRepository dateTimeSettingRepository;

        public TreesController(ApplicationContext context)
        {
            treeRepository = new TreeRepository(context);
            updateService = new TreeUpdateService(context);
            dateTimeSettingRepository = new DateTimeSettingRepository(context);
        }

        [HttpGet]
        public JsonResult Public()
        {
            var trees = treeRepository.GetPublicTrees()
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
            var trees = treeRepository.GetUserGenTrees(authorizedUserId)
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
            int authorizedUserId = 0;
            if (HttpContext.User.Identity.IsAuthenticated)
                authorizedUserId = int.Parse(HttpContext.User.Identity.Name);

            var tree = treeRepository.GetTree(id, authorizedUserId, forUpdate: false);

            if (tree == null)
                return BadRequest($"Unable to access tree with id {id}");

            var treeModel = new ModelEntityConverter().ToViewModel(tree);
            treeModel.CanEdit = tree.Owner.Id == authorizedUserId;

            return Ok(JsonConvert.SerializeObject(treeModel));
        }

        public IActionResult AddTree(GenTreeViewModel model)
        {
            return ProcessDBOperation((userId, replacements) =>
                treeRepository.Add(model, userId, replacements));
        }

        public IActionResult UpdateTree(GenTreeViewModel model)
        {
            return ProcessDBOperation((userId, replacements) =>
                treeRepository.Update(model, userId, replacements));
        }

        [HttpPost]
        public IActionResult AddSetting(GenTreeDateTimeSetting model)
        {
            return ProcessDBOperation((userId, replacements) =>
                dateTimeSettingRepository.Add(model, userId, replacements));
        }

        [HttpPost]
        public IActionResult UpdateOrAddSetting(GenTreeDateTimeSetting model)
        {
            return ProcessDBOperation((userId, replacements) =>
                dateTimeSettingRepository.UpdateOrAdd(model, userId, replacements));
        }

        [HttpPost]
        public IActionResult Update(CustomPersonDescriptionTemplate model)
        {
            treeRepository.Update(model, model.Id);
            return Ok();
        }

        /// <summary>
        /// Выполняет проверку авторизации и действие с базой данных, требующее id авторизованного пользователя и заполняющее словарь изменений
        /// </summary>
        /// <param name="DBaction">Действие с базой данных, в который передается id авторизованного пользователя и словарь замен</param>
        /// <returns>
        /// <list type="bullet">
        /// <item>BadRequest, если пользователь не авторизован;</item>
        /// <item>Ok со списком ошибок и замен в body после выполнения действия с БД</item>
        /// </list>
        /// </returns>
        private IActionResult ProcessDBOperation(Action<int, Dictionary<int, IIdentified>> DBaction)
        {
            int authorizedUserId;
            if (HttpContext.User.Identity.IsAuthenticated)
                authorizedUserId = int.Parse(HttpContext.User.Identity.Name);
            else
                return BadRequest("not logged in");

            var replacements = new Dictionary<int, IIdentified>();
            DBaction(authorizedUserId, replacements);

            return Ok(JsonConvert.SerializeObject(
                new Changes 
                { 
                    Replacements = replacements.ToDictionary(x => x.Key, x => x.Value.Id) 
                }));
        }
    }
}