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

        public TreesController(ApplicationContext context)
        {
            treesService = new TreesService(context);
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
                    Description = ShortenDescription(tree.Description, 100),
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
                    Description = ShortenDescription(tree.Description, 100),
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

            var treeModel = ToViewModel(tree);
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
                //обновление дерева
                tree.Name = model.Name;
                tree.Description = model.Description;
                //TODO is private
                tree.LastUpdated = DateTime.Now;
                tree.Image = model.Image;

                //обновление шаблонов описаний
                model.DescriptionTemplates.ForEach(templateModel =>
                {
                //получаем соответствующий шаблон описания, который сейчас в бд
                var template = tree.CustomPersonDescriptionTemplates
                        .FirstOrDefault(t => t.Id == templateModel.Id);
                //если такового не было найдено, добавляем
                if (template == null)
                        tree.CustomPersonDescriptionTemplates.Add(template);
                    else
                    {
                    //измением свойства
                    template.Name = templateModel.Name;
                        template.Type = templateModel.Type;
                    }
                });

                //удаляем шаблоны, которые были удалены в модели
                tree.CustomPersonDescriptionTemplates
                    .Select(e => e.Id)
                    .Where(id => model.DescriptionTemplates
                       .FirstOrDefault(m => m.Id == id) == null)
                    .ToList()
                    .ForEach(id =>
                    {
                    //удаляем все описания, которые были для шаблона
                    tree.Persons.ForEach(p => p.CustomDescriptions.RemoveAll(d => d.Template.Id == id));
                    //удаляем сам шаблон
                    tree.CustomPersonDescriptionTemplates.RemoveAll(t => t.Id == id);
                    });

                //обновление людей
                model.Persons.ForEach(personModel =>
                {
                //получаем данные о соответствующем человеке, хранящиеся в бд
                var person = tree.Persons.FirstOrDefault(p => p.Id == personModel.Id);
                //если такового не нашлось, добавляем
                if (person == null)
                        tree.Persons.Add(new Person
                        {
                            LastName = personModel.LastName,
                            FirstName = personModel.FirstName,
                            MiddleName = personModel.MiddleName,
                        //TO DO: добавить Birth Place
                        Biography = personModel.Biography,
                            Gender = personModel.Gender,
                            Image = personModel.Image,
                            CustomDescriptions = personModel.CustomDescriptions
                        });
                    else
                    {
                        person.LastName = personModel.LastName;
                        person.FirstName = personModel.FirstName;
                        person.MiddleName = personModel.MiddleName;
                        person.Biography = personModel.Biography;
                        person.Gender = personModel.Gender;
                        person.Image = personModel.Image;
                    //обновляем пользовательские описания
                    personModel.CustomDescriptions.ForEach(descriptionModel =>
                        {
                        //получаем соответствующее описание, которое есть в бд
                        var description = person.CustomDescriptions.FirstOrDefault(d => d.Id == descriptionModel.Id);
                        //получаем шаблон описания
                        var template = tree.CustomPersonDescriptionTemplates.FirstOrDefault(t => t.Id == descriptionModel.Template.Id);
                        //если указан несуществующий шаблон - игнорируем
                        if (template == null) return;
                        //если описания нет, добавляем
                        if (description == null)
                                person.CustomDescriptions.Add(description);
                            else
                            {
                                description.Template = template;
                                description.Value = descriptionModel.Value;
                            }
                        });

                    //TO DO: удаление удаленны шаблонов
                    //обновляем отношения
                    personModel.Relations.ForEach(relationModel =>
                        {
                        //получаем соответсвующую связь, которая есть в бд
                        var relation = person.Relations.FirstOrDefault(r => r.Id == relationModel.Id);
                        //добавляем связь если таковой нет
                        if (relation == null)
                            {
                                if (relationModel is SpouseRelationViewModel)
                                    person.Relations.Add(new SpouseRelation
                                    {
                                        TargetPerson = tree.Persons.FirstOrDefault(p => p.Id == relationModel.TargetPersonId),
                                        IsFinished = (relationModel as SpouseRelationViewModel).IsFinished
                                    });
                                else if (relationModel is ChildRelationViewModel)
                                    person.Relations.Add(new ChildRelation
                                    {
                                        TargetPerson = tree.Persons.FirstOrDefault(p => p.Id == relationModel.TargetPersonId),
                                        RelationRate = (RelationRate)Enum.Parse(typeof(RelationRate), (relationModel as ChildRelationViewModel).RelationRate),
                                        SecondParent = tree.Persons.FirstOrDefault(p => p.Id == (relationModel as ChildRelationViewModel).SecondParentId)
                                    });
                            }
                            else
                            {
                                if (relationModel is SpouseRelationViewModel && relation is SpouseRelation)
                                    (relation as SpouseRelation).IsFinished = (relationModel as SpouseRelationViewModel).IsFinished;
                            }
                        });
                    }
                });
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

        private GenTreeViewModel ToViewModel(GenTree tree)
        {
            var treeModel = new GenTreeViewModel
            {
                Id = tree.Id,
                Name = tree.Name,
                Description = tree.Description,
                Creator = tree.Owner.Login,
                DateCreated = tree.DateCreated.ToString("d/MM/yyyy"),
                LastUpdated = tree.LastUpdated.ToString("d/MM/yyyy"),
                Image = tree.Image,
                DescriptionTemplates = tree.CustomPersonDescriptionTemplates
            };

            if (tree.Persons != null)
                treeModel.Persons = tree.Persons.Select(p => ToViewModel(p)).ToList();

            return treeModel;
        }

        private PersonViewModel ToViewModel(Person person)
        {
            var personModel = new PersonViewModel
            {
                Id = person.Id,
                LastName = person.LastName,
                FirstName = person.FirstName,
                MiddleName = person.MiddleName,
                Gender = person.Gender,
                Biography = person.Biography,
                Image = person.Image,
                CustomDescriptions = person.CustomDescriptions
            };
            if (person.BirthDate != null)
            {
                personModel.BirthDate = person.BirthDate.ToDateTimeString();
                personModel.ShortBirthDate = person.BirthDate.ToShortDateTimeString();
            }
            if (person.DeathDate != null)
            {
                personModel.DeathDate = person.DeathDate.ToDateTimeString();
                personModel.ShortDeathDate = person.DeathDate.ToShortDateTimeString();
            }

            if (person.Relations != null)
                personModel.Relations = person.Relations.Select(r => ToViewModel(r)).ToList();
            return personModel;
        }

        private RelationViewModel ToViewModel(Relation relation)
        {
            if (relation is ChildRelation)
                return ToViewModel(relation as ChildRelation);
            else
                return ToViewModel(relation as SpouseRelation);
        }
        private ChildRelationViewModel ToViewModel(ChildRelation relation)
        {
            var childRelationModel = new ChildRelationViewModel
            {
                Id = relation.Id,
                TargetPersonId = relation.TargetPerson.Id,
                SecondParentId = null,
                RelationRate = relation.RelationRate.ToString(),
                RelationType = "ChildRelation"
            };
            if (relation.SecondParent != null)
                childRelationModel.SecondParentId = relation.SecondParent.Id;
            return childRelationModel;
        }

        private SpouseRelationViewModel ToViewModel(SpouseRelation relation)
        {
            return new SpouseRelationViewModel
            {
                Id = relation.Id,
                TargetPersonId = relation.TargetPerson.Id,
                IsFinished = relation.IsFinished,
                RelationType = "SpouseRelation"
            };
        }

        private string ShortenDescription(string description, int length)
        {
            if (description == null)
                return null;

            return description.Substring(0, Math.Min(length, description.Length));
        }
    }
}