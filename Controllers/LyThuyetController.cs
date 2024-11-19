using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json.Serialization;
using System.Text.Json;
using DemoGPLX.Models;
using Newtonsoft.Json;
using apigplx.ModelVM;
using Microsoft.Extensions.Caching.Memory;

namespace DemoGPLX.Controllers
{
    public class LyThuyetController : Controller
    {
        private readonly IMemoryCache _cache;

        public LyThuyetController(IMemoryCache memoryCache)
        {
            _cache = memoryCache;
        }

        JsonSerializerOptions opt = new JsonSerializerOptions()
        {
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.Preserve
        };

        CookieOptions options = new CookieOptions() { Expires = DateTime.Now.AddDays(1) };
        public IActionResult Index()
        {
            return View();
        }


        public IActionResult Hoc()
        {
            if (HttpContext.Request.Cookies["DataUserGPLX"] == null)
            {
                return RedirectToAction("ChonHang");
            }
            DataUser user = JsonConvert.DeserializeObject<DataUser>(HttpContext.Request.Cookies["DataUserGPLX"]);

            ViewBag.Cookies = user.Id;
            return View();
        }

        public IActionResult Thi()
        {
            if (HttpContext.Request.Cookies["DataUserGPLX"] == null)
            {
                return RedirectToAction("ChonHang");
            }
            DataUser user = JsonConvert.DeserializeObject<DataUser>(HttpContext.Request.Cookies["DataUserGPLX"]);
            ViewBag.Cookies = user.Id;
            ViewBag.setTest = true;
            ViewBag.time = new DbGplxContext().Hangs.Where(e => e.IdHang == int.Parse(user.Id)).FirstOrDefault().Thoigianthi;
            return View();
        }

        public IActionResult CauHaySai()
        {
            DataUser user = null;
            try
            {
                user = JsonConvert.DeserializeObject<DataUser>(HttpContext.Request.Cookies["DataUserGPLX"]);
            }
            catch (Exception ex)
            { Console.WriteLine(ex.Message); }

            if (user == null)
                return RedirectToAction("ChonHang");
            ViewBag.data = user;
            return View(user);

        }

        public IActionResult XoaDuLieuCauSai()
        {
            DataUser data = JsonConvert.DeserializeObject<DataUser>(HttpContext.Request.Cookies["DataUserGPLX"]);
            data.CauSais = new List<int>();
            HttpContext.Response.Cookies.Append("DataUserGPLX", JsonConvert.SerializeObject(data), options);
            return RedirectToAction("CauHaySai");
        }

        public IActionResult ChonHang()
        {
            const string cacheKey = "AllHang";
            if (!_cache.TryGetValue(cacheKey, out List<Hang> hangList))
            {
                // Cache không tồn tại, lấy dữ liệu từ database
                hangList = QuestionUtil.GetAllHang();

                // Cấu hình cache (thời gian sống: 1 ngày)
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromDays(1));
                _cache.Set(cacheKey, hangList, cacheOptions);

                Console.WriteLine("Cache Miss: Dữ liệu được lấy từ database.");
            }
            else
            {
                Console.WriteLine("Cache Hit: Dữ liệu được lấy từ cache.");
            }

            ViewBag.hang = hangList;
            return View(hangList);
        }


        public IActionResult ChonHangThi(string id)
        {
            HttpContext.Response.Cookies.Delete("DataUserGPLX");
            DataUser data = new DataUser();
            data.Id = id;
            HttpContext.Response.Cookies.Append("DataUserGPLX", JsonConvert.SerializeObject(data), options);
            return RedirectToAction("Hoc");
        }

        [HttpPost]
        public JsonResult LayCauHoi(int id)
        {
            const string cacheKeyPrefix = "QuestionData_";
            string cacheKey = $"{cacheKeyPrefix}{id}";

            // Kiểm tra dữ liệu câu hỏi trong cache
            if (!_cache.TryGetValue(cacheKey, out List<Cau> data))
            {
                // Nếu chưa có, gọi phương thức lấy dữ liệu câu hỏi
                data = QuestionUtil.GetDataWithID(id);

                // Lưu dữ liệu vào cache với thời gian sống 10 phút (tùy chỉnh theo yêu cầu)
                _cache.Set(cacheKey, data, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });
            }

            // Trả về dữ liệu từ cache hoặc vừa truy xuất
            return Json(data, opt);
        }


        [HttpPost]
        public IActionResult ThemCau(int id, int index)
        {
            try
            {
                DataUser data = JsonConvert.DeserializeObject<DataUser>(HttpContext.Request.Cookies["DataUserGPLX"]);

                if (data.Caus.IndexOf(id) == -1)
                {
                    data.Caus.Add(id);
                    data.LuaChons.Add(index);
                }

                // Update user data
                HttpContext.Response.Cookies.Append("DataUserGPLX", JsonConvert.SerializeObject(data), options);

                // Optionally, return the updated data as JSON
                return Json(data);
            }
            catch (Exception )
            {
                // Handle exceptions appropriately
                return BadRequest("An error occurred while updating user data.");
            }
        }

        [HttpPost]
        public IActionResult ThemCauSai(int id)
        {
            try
            {
                DataUser data = JsonConvert.DeserializeObject<DataUser>(HttpContext.Request.Cookies["DataUserGPLX"]);

                if (data.CauSais.IndexOf(id) == -1)
                    data.CauSais.Add(id);
                // Update user data
                HttpContext.Response.Cookies.Append("DataUserGPLX", JsonConvert.SerializeObject(data));

                // Optionally, return the updated data as JSON
                return Json(data);
            }
            catch (Exception ex)
            {
                // Handle exceptions appropriately
                return BadRequest("An error occurred while updating user data.");
            }
        }

        public IActionResult LayCauSai()
        {
            if (HttpContext.Request.Cookies["DataUserGPLX"] == null)
            {
                return RedirectToAction("ChonHang");
            }
            DataUser user = JsonConvert.DeserializeObject<DataUser>(HttpContext.Request.Cookies["DataUserGPLX"]);
            List<Cau> lsCauHaySai = new List<Cau>();
            foreach (int i in user.CauSais)
            {
                lsCauHaySai.Add(QuestionUtil.GetQuestionWithoutImage(i));
            }
            return Json(lsCauHaySai, opt);
        }
        [HttpPost]
        public IActionResult LayDeThiMoi(int id)
        {
            const string cacheKeyPrefix = "LastTestTime_";
            string cacheKey = $"{cacheKeyPrefix}{id}";

            // Kiểm tra trong cache xem đã đủ 5 phút chưa
            if (_cache.TryGetValue(cacheKey, out DateTime lastTestTime))
            {
                if (DateTime.Now < lastTestTime.AddMinutes(10))
                {
                    var remainingTime = lastTestTime.AddMinutes(10) - DateTime.Now;
                    return BadRequest($"Bạn cần chờ thêm {remainingTime.Minutes} phút {remainingTime.Seconds} giây để lấy đề thi mới.");
                }
            }

            // Lấy danh sách câu hỏi mới
            var questions = QuestionUtil.CreateNewTest(id);

            // Xáo trộn thứ tự câu hỏi
            var random = new Random();
            questions = questions.OrderBy(x => random.Next()).ToList();

            // Lưu lại thời gian lấy đề thi vào cache
            _cache.Set(cacheKey, DateTime.Now, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            return Json(questions, opt);
        }


        [HttpPost]
        public IActionResult LayCauDaLam()
        {
            List<List<int>> ls = new List<List<int>>() {
                new List<int>(JsonConvert.DeserializeObject<DataUser>(HttpContext.Request.Cookies["DataUserGPLX"]).Caus),
                new List<int>(JsonConvert.DeserializeObject<DataUser>(HttpContext.Request.Cookies["DataUserGPLX"]).LuaChons)
            };


            return Json(ls);
        }


        [HttpPost]
        public IActionResult LayHinhCauHoi(int id)
        {
            const string cacheKeyPrefix = "ImageQuestion_";
            string cacheKey = $"{cacheKeyPrefix}{id}";

            // Kiểm tra xem hình ảnh đã được lưu trong cache chưa
            if (!_cache.TryGetValue(cacheKey, out object imageData))
            {
                // Nếu chưa có, gọi phương thức lấy hình ảnh
                imageData = QuestionUtil.GetImageQuestion(id);

                // Lưu vào cache với thời gian sống 1 giờ
                _cache.Set(cacheKey, imageData, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });
            }

            // Trả về hình ảnh từ cache hoặc vừa truy xuất
            return Json(imageData);
        }


        public IActionResult SearchQuestion(string search, bool choose = false)
        {
            const string cacheKey = "AllQuestions";
            if (!_cache.TryGetValue(cacheKey, out List<Cau> lsAllQues))
            {
                lsAllQues = QuestionUtil.GetAllQuestion();
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(1));
                _cache.Set(cacheKey, lsAllQues, cacheOptions);
            }

            search = AppUtil.ToLowerCaseNonAccentVietnamese(search.ToLower());
            List<Cau> lsQues = new List<Cau>();

            foreach (var c in lsAllQues)
            {
                if (AppUtil.ToLowerCaseNonAccentVietnamese(c.Ttcaus.ElementAt(0).Cauhoi.ToLower()).Contains(search))
                {
                    lsQues.Add(c);
                }
                else
                {
                    foreach (var dapan in c.Dapans)
                    {
                        if (AppUtil.ToLowerCaseNonAccentVietnamese(dapan.Dapan1.ToLower()).Contains(search))
                        {
                            lsQues.Add(c);
                            break;
                        }
                    }
                }
            }

            return Json(lsQues, opt);
        }
    }
}