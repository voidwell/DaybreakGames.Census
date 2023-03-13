using DaybreakGames.Census;
using DemoApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DemoApp
{
    [Route("/")]
    public class DemoController : Controller
    {
        private readonly ICensusQueryFactory _censusFactory;

        public DemoController(ICensusQueryFactory censusFactory)
        {
            _censusFactory = censusFactory;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("character")]
        public async Task<IActionResult> GetCharacter(string characterName)
        {
            var names = characterName.ToLower().Split(',').Select(a => a.Trim()).ToArray();

            var query = _censusFactory.Create("character")
                .Where("name.first_lower", a => a.Equals(names))
                .SetLimit(10);

            var uu = query.GetUri();

            // In this example GetListAsync is passing a model to bind the response items to
            var characterList = await query.GetListAsync<CensusCharacterModel>();

            ViewData["CharacterList"] = JsonSerializer.Serialize(characterList);

            return View("Index");
        }

        [HttpPost("outfit")]
        public async Task<IActionResult> GetOutfit(string outfitAlias)
        {
            var query = _censusFactory.Create("outfit");
            query.Where("alias").Equals(outfitAlias);

            // If no model is specified in the Get request then it's returned as a JToken
            var outfit = await query.GetAsync();

            ViewData["Outfit"] = JsonSerializer.Serialize(outfit).ToString();

            return View("Index");
        }
    }
}
