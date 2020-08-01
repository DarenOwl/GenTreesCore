﻿using System.Linq;
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