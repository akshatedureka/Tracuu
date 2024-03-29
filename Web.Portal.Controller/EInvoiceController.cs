﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Web.Portal.Common.ViewModel;
using Web.Portal.Common;
using Web.Portal.Service;
using Web.Portal.Model.Models;
using Web.Portal.DataAccess;
using System.Web.Script.Serialization;
using MoralesLarios.Linq;
using Newtonsoft.Json;
using Web.Portal.Utils;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Http;
using Web.Portal.Controller.vn.cinvoice.api;
using Web.Portal.Common.ViewModel.eInvoiceViewModel;
using System.Threading;

namespace Web.Portal.Controller
{
    [Web.Portal.Sercurity.AuthorizedBase(Roles = "ADMIN,KTN")]
    public class EInvoiceController : BaseController
    {
        DateTime? dateCheck;
        IHermesInvoiceService _iHermesInvoiceService;
        IHermesInvoiceDetailService _iHermesInvoiceDetailService;
        IPrintService _iPrintService;
        IActionLogService _iActionLogService;
        IResponseMessageService _iResponseMessageService;

        public EInvoiceController(IHermesInvoiceService iHermesInvoiceService,
            IHermesInvoiceDetailService iHermesInvoiceDetailService, IPrintService iPrintService,
            IActionLogService iActionLogService, IResponseMessageService iResponseMessageService)
        {
            this._iHermesInvoiceService = iHermesInvoiceService;
            this._iHermesInvoiceDetailService = iHermesInvoiceDetailService;
            this._iPrintService = iPrintService;
            this._iActionLogService = iActionLogService;
            this._iResponseMessageService = iResponseMessageService;
        }
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult List()
        {
            ShowData(false);

            return View();
        }
        public void ShowData(bool export)
        {
            int total = 0;
            int totalRecord = 0;
            int totalHermes = 0;
            string no = string.IsNullOrEmpty(Request["no"]) ? string.Empty : Request["no"].Trim();
            string awb = string.IsNullOrEmpty(Request["awb"]) ? string.Empty : Request["awb"].Trim();
            string hawb = string.IsNullOrEmpty(Request["hawb"]) ? string.Empty : Request["hawb"].Trim();
            //string type = string.IsNullOrEmpty(Request["ty"]) ? "ALL" : Request["ty"].Trim();
            string staff = string.IsNullOrEmpty(Request["staff"]) ? "ALL" : Request["staff"].Trim();
            int status = int.Parse(Request["status"]);
            int page = string.IsNullOrEmpty(Request["page"]) ? 1 : Convert.ToInt32(Request["page"]);
            int pageSize = string.IsNullOrEmpty(Request["ps"]) ? 20 : Convert.ToInt32(Request["ps"]);
            dateCheck = string.IsNullOrEmpty(Request["date"]) ? dateCheck : Web.Portal.Utils.Format.ConvertDate(Request["date"]);
            List<HermesInvoice> Invoices = new List<HermesInvoice>();
            if (export)
            {
                Invoices = _iHermesInvoiceService.GetListPaging(dateCheck.Value, no, awb, hawb, staff, status, page,
                                                                             pageSize,
                                                                            ref total, "IMPORT AWB", ref totalRecord, ref totalHermes).OrderBy(c => c.Sequence).ToList();
                decimal totalBank = Invoices.Where(c => c.Payment == "BANK TFR").ToList().Sum(x => x.InvoiceTotalAmount) - Invoices.Where(c => c.PaymentDescription.ToUpper() == "POS" && c.Payment.Trim() != "CASH").ToList().Sum(x => x.InvoiceTotalAmount);
                ViewBag.TotalCash = Invoices.Where(c => c.Payment == "CASH").ToList().Sum(x => x.InvoiceTotalAmount).ToString("N2"); ;
                ViewBag.TotalBank = totalBank.ToString("N2");
                ViewBag.TotalPOS = Invoices.Where(c => c.PaymentDescription.ToUpper() == "POS" ).ToList().Sum(x => x.InvoiceTotalAmount).ToString("N2");
                ViewBag.DateChek = dateCheck.Value.ToString("dd/MM/yyyy");
            }
            else
            {
                Invoices = _iHermesInvoiceService.GetListPaging(dateCheck.Value, no, awb, hawb, staff, status, page,
                                                                             pageSize,
                                                                             ref total, "IMPORT AWB", ref totalRecord);
            }
           
            ViewData["InvoiceLists"] = Invoices;
            ViewBag.TotalRecord = totalRecord;
            ViewBag.TotalFilter = total;
            ViewBag.TotalHermes = totalHermes;
            ViewBag.TotalNoVat = Invoices.Sum(x => x.InvoiceTotalNoVatAmount).ToString("N2");
            ViewBag.TotalVat = Invoices.Sum(x => x.InvoiceTotalVatAmount).ToString("N2");
            ViewBag.Total = Invoices.Sum(x => x.InvoiceTotalAmount).ToString("N2");
          
            //ViewBag.Paging = Utils.DisplayMessage.CreatePaging("pagingeinv", total, page, pageSize);
        }
        [DocumentExport("EXCEL", "EXP_BAOCAO_THANHTOAN")]
        public ActionResult ImpExport()
        {
            ShowData(true);
            return View();

        }
        public ActionResult InvoiceDetail()
        {
            string invoiceIsn = Request["invoiceIsn"].Trim();
            HermesInvoice hermesInvoice = _iHermesInvoiceService.GetByInvoiceIsn(invoiceIsn);
            List<HermesInvoiceDetail> listInvoiceDetail = _iHermesInvoiceDetailService.GetByInvoiceIsn(invoiceIsn.ToString()).ToList();
            ViewBag.TotalRecord = listInvoiceDetail.Count;
            ViewData["InvoiceDetailLists"] = listInvoiceDetail;
            return View(hermesInvoice);
        }
        public ActionResult Edit()
        {
            string invoiceIsn = Request["invoiceIsn"].Trim();
            HermesInvoice hermesInvoice = _iHermesInvoiceService.GetByInvoiceIsn(invoiceIsn);
            List<HermesInvoiceDetail> listInvoiceDetail = _iHermesInvoiceDetailService.GetByInvoiceIsn(invoiceIsn.ToString()).ToList();
            List<OrderDetailViewModel> listOrderDetails = new List<OrderDetailViewModel>();
            foreach (var item in listInvoiceDetail)
            {
                OrderDetailViewModel orderDetail = new OrderDetailViewModel();
                orderDetail.ID = item.ID;
                orderDetail.InvoiceIns = item.InvoiceIns;
                orderDetail.InvoiceLineIns = item.InvoiceLineIns;
                orderDetail.Item = item.Item;
                orderDetail.Amount = item.Amount;
                orderDetail.Quantity = item.Quantity;
                orderDetail.Tax = int.Parse(item.TaxRate);
                orderDetail.UnitPrice = item.UnitPrice;
                orderDetail.VAT = item.VAT;
                orderDetail.Unit = item.Unit;
                orderDetail.Action = 0;
                listOrderDetails.Add(orderDetail);
            }
            Session[CommonConstants.SessionAdmin] = listOrderDetails;
            ViewBag.TotalRecord = listInvoiceDetail.Count;
            ViewData["InvoiceDetailLists"] = listInvoiceDetail;
            return View(hermesInvoice);
        }
        public ActionResult Replace()
        {
            string invoiceIsn = Request["invoiceIsn"].Trim();
            HermesInvoice hermesInvoice = _iHermesInvoiceService.GetByInvoiceIsn(invoiceIsn);
            List<HermesInvoiceDetail> listInvoiceDetail = _iHermesInvoiceDetailService.GetByInvoiceIsn(invoiceIsn.ToString()).ToList();
            List<OrderDetailViewModel> listOrderDetails = new List<OrderDetailViewModel>();
            foreach (var item in listInvoiceDetail)
            {
                OrderDetailViewModel orderDetail = new OrderDetailViewModel();
                orderDetail.ID = item.ID;
                orderDetail.InvoiceIns = item.InvoiceIns;
                orderDetail.InvoiceLineIns = item.InvoiceLineIns;
                orderDetail.Item = item.Item;
                orderDetail.Amount = item.Amount;
                orderDetail.Quantity = item.Quantity;
                orderDetail.Tax = int.Parse(item.TaxRate);
                orderDetail.UnitPrice = item.UnitPrice;
                orderDetail.VAT = item.VAT;
                orderDetail.Unit = item.Unit;
                orderDetail.Action = 0;
                listOrderDetails.Add(orderDetail);
            }
            Session[CommonConstants.SessionAdmin] = listOrderDetails;
            ViewBag.TotalRecord = listInvoiceDetail.Count;
            ViewData["InvoiceDetailLists"] = listInvoiceDetail;
            return View(hermesInvoice);
        }
        public ActionResult SaveReplace(string order)
        {
            string message = string.Empty;
            string messageType = Utils.DisplayMessage.TypeSuccess;
            try
            {
                var OrderViewModel = new JavaScriptSerializer().Deserialize<InvoiceViewModel>(order);
                var invoiceDB = _iHermesInvoiceService.GetByID(OrderViewModel.ID);
                if (invoiceDB.IsCancel.HasValue && invoiceDB.IsCancel.Value == true)
                {
                    invoiceDB.InvoiceIsn = "NEW" + invoiceDB.InvoiceIsn;
                    invoiceDB.KundName = OrderViewModel.KundName.Trim();
                    invoiceDB.Address = OrderViewModel.Address.Trim();
                    invoiceDB.TaxCode = OrderViewModel.TaxCode.Trim();
                    invoiceDB.AWB = OrderViewModel.AWB.Trim();
                    invoiceDB.Hawb = OrderViewModel.Hawb.Trim();
                    invoiceDB.Note = invoiceDB.Note + OrderViewModel.Note.Trim() + " Xuất lại từ Tra cứu";
                    invoiceDB.Email = OrderViewModel.Email.Trim();
                    foreach (var item in OrderViewModel.InvoiceDetailViewModels)
                    {
                        var invoiceDetailDB = _iHermesInvoiceDetailService.GetByID(item.ID);
                        invoiceDetailDB.Quantity = item.Quantity;
                        invoiceDetailDB.UnitPrice = item.UnitPrice;
                        invoiceDetailDB.InvoiceIns = invoiceDB.InvoiceIsn;
                        _iHermesInvoiceDetailService.Add(invoiceDetailDB);
                    }
                    _iHermesInvoiceService.Add(invoiceDB);
                    _iHermesInvoiceService.Save();
                }

                string jsonResult = JsonInvoiceALSC(invoiceDB);
                string rp = "";
                Utils.HttpRequest rq = new Utils.HttpRequest();
                string url = System.Configuration.ConfigurationManager.AppSettings["URL_Appr"];
                rq.Url = url;
                bool check = false;
                var watch = System.Diagnostics.Stopwatch.StartNew();

                // the code that you want to measure comes here
                invoiceDB.TimeSent = DateTime.Now;

                rp = rq.Execute(jsonResult, "POST", "", false, "", ref check);
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                if (check)
                {
                    JObject jObj = JObject.Parse(rp);                 // Parse the object graph
                    int seq = int.Parse(jObj["seq"].ToString());
                    string reference = jObj["id"].ToString();
                    string invoiceSearchLink = jObj["link"].ToString();
                    string searchCode = jObj["sec"].ToString();
                    invoiceDB.Status = true;
                    invoiceDB.TimeReponse = DateTime.Now;
                    invoiceDB.ExecuteTime = elapsedMs.ToString();
                    invoiceDB.Sequence = seq;
                    invoiceDB.ReferenceNo = reference;
                    invoiceDB.InvoiceStatus = CommonConstants.INVOICEAPROVE;
                    invoiceDB.InvoiceDescription = CommonConstants.APPROVE;
                    invoiceDB.EInvoiceSearchLink = invoiceSearchLink;
                    invoiceDB.SearchCode = searchCode;
                    _iHermesInvoiceService.Update(invoiceDB);
                    _iHermesInvoiceService.Save();
                    return Json(new
                    {
                        data = invoiceDB.InvoiceIsn,
                        status = true
                    }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new
                    {
                        data = rp,
                        status = false
                    }, JsonRequestBehavior.AllowGet);
                }

            }
            catch (Exception ex)
            {
                messageType = Utils.DisplayMessage.TypeError;
                Log.WriteLog(ex.ToString(), "InvoiceEdit.txt");
                message = ex.ToString();
                return Json(new { Type = messageType, Message = message, Title = "Thông báo" }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult SaveEdit(string order)
        {
            string message = string.Empty;
            string messageType = Utils.DisplayMessage.TypeSuccess;
            try
            {
                var OrderViewModel = new JavaScriptSerializer().Deserialize<InvoiceViewModel>(order);
                var invoiceDB = _iHermesInvoiceService.GetByID(OrderViewModel.ID);
                invoiceDB.KundName = OrderViewModel.KundName.Trim();
                invoiceDB.Address = OrderViewModel.Address.Trim();
                invoiceDB.TaxCode = OrderViewModel.TaxCode.Trim();
                invoiceDB.AWB = OrderViewModel.AWB.Trim();
                invoiceDB.Hawb = OrderViewModel.Hawb.Trim();
                invoiceDB.Note = invoiceDB.Note + OrderViewModel.Note.Trim() + " Xuất lại từ Tra cứu";
                invoiceDB.Email = OrderViewModel.Email.Replace(" ", "").Trim();
                foreach (var item in OrderViewModel.InvoiceDetailViewModels)
                {
                    var invoiceDetailDB = _iHermesInvoiceDetailService.GetByID(item.ID);
                    invoiceDetailDB.Quantity = item.Quantity;
                    invoiceDetailDB.UnitPrice = item.UnitPrice;
                    _iHermesInvoiceDetailService.Update(invoiceDetailDB);
                }
                _iHermesInvoiceService.Update(invoiceDB);
                _iHermesInvoiceService.Save();
                string jsonResult = JsonInvoiceALSC(invoiceDB);
                string rp = "";
                Utils.HttpRequest rq = new Utils.HttpRequest();
                string url = System.Configuration.ConfigurationManager.AppSettings["URL_Appr"];
                rq.Url = url;
                bool check = false;
                var watch = System.Diagnostics.Stopwatch.StartNew();

                // the code that you want to measure comes here
                invoiceDB.TimeSent = DateTime.Now;
                bool exception = false;
                rp = rq.ExecuteInvoice(jsonResult, "POST", "", false, "", ref check,ref exception);
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                if (check && !exception)
                {
                    JObject jObj = JObject.Parse(rp);                 // Parse the object graph
                    int seq = int.Parse(jObj["seq"].ToString());
                    string reference = jObj["id"].ToString();
                    string invoiceSearchLink = jObj["link"].ToString();
                    string searchCode = jObj["sec"].ToString();
                    invoiceDB.Status = true;
                    invoiceDB.TimeReponse = DateTime.Now;
                    invoiceDB.ExecuteTime = elapsedMs.ToString();
                    invoiceDB.Sequence = seq;
                    invoiceDB.ReferenceNo = reference;
                    invoiceDB.InvoiceStatus = CommonConstants.INVOICEAPROVE;
                    invoiceDB.InvoiceDescription = CommonConstants.APPROVE;
                    invoiceDB.ExceptionStatus = 0;
                    invoiceDB.EInvoiceSearchLink = invoiceSearchLink;
                    invoiceDB.SearchCode = searchCode;
                    _iHermesInvoiceService.Update(invoiceDB);
                    _iHermesInvoiceService.Save();
                    return Json(new
                    {
                        data = invoiceDB.InvoiceIsn,
                        status = true
                    }, JsonRequestBehavior.AllowGet);
                }
                else if (check && exception)
                {
                    JArray jsonArray = JArray.Parse(rp);
                    string sequence = jsonArray[0]["doc"]["seq"].ToString();
                    string invoiceSearchLink = jsonArray[0]["doc"]["link"].ToString();
                    string searchCode = jsonArray[0]["doc"]["sec"].ToString();
                    invoiceDB.Status = true;
                    invoiceDB.TimeReponse = DateTime.Now;
                    invoiceDB.ExecuteTime = elapsedMs.ToString();
                    invoiceDB.Sequence = int.Parse(sequence);
                    invoiceDB.ReferenceNo = jsonArray[0]["doc"]["id"].ToString();
                    invoiceDB.InvoiceStatus = CommonConstants.INVOICEAPROVE;
                    invoiceDB.InvoiceDescription = CommonConstants.APPROVE;
                    invoiceDB.EInvoiceSearchLink = invoiceSearchLink;
                    invoiceDB.SearchCode = searchCode;
                    invoiceDB.ExceptionStatus = 0;
                    _iHermesInvoiceService.Update(invoiceDB);
                    _iHermesInvoiceService.Save();
                    return Json(new
                    {
                        data = invoiceDB.InvoiceIsn,
                        status = true
                    }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new
                    {
                        data = rp,
                        status = false
                    }, JsonRequestBehavior.AllowGet);
                }

            }
            catch (Exception ex)
            {
                messageType = Utils.DisplayMessage.TypeError;
                Log.WriteLog(ex.ToString(), "InvoiceEdit.txt");
                message = ex.ToString();
                return Json(new { Type = messageType, Message = message, Title = "Thông báo" }, JsonRequestBehavior.AllowGet);
            }

        }
        public ActionResult CancelInvoice()
        {
            bool check = false;
            string message = string.Empty;
            string messageType = Utils.DisplayMessage.TypeSuccess;
            string invoiceIsn = Request["invoiceIsn"].Trim();
            try
            {
                var invoice = _iHermesInvoiceService.GetByInvoiceIsn(invoiceIsn);
                if (invoice != null && invoice.InvoiceStatus != 3)
                {
                    Account account = new Account();
                    account.username = System.Configuration.ConfigurationManager.AppSettings["UserNameEInvoiceALSC"];
                    account.password = System.Configuration.ConfigurationManager.AppSettings["PasswordEInvoiceALSC"];
                    adj adj = new adj();
                    adj.rdt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                    adj.rea = "Sai thong tin hoa don";
                    adj.ref_ = invoice.InvoiceIsn;
                    inv inv = new inv();
                    inv.seq = System.Configuration.ConfigurationManager.AppSettings["SeqCancel_IMPORT"] + invoice.Sequence.ToString().PadLeft(7, '0'); ;
                    inv.adj = adj;
                    InvoiceCancel invoiceCancel = new InvoiceCancel();
                    invoiceCancel.user = account;
                    invoiceCancel.inv = inv;
                    string jsonResult = JsonConvert.SerializeObject(invoiceCancel);
                    string jsonInvoiceCancel = jsonResult.Replace("ref_", "ref");
                    Utils.HttpRequest rq = new Utils.HttpRequest();
                    string url = System.Configuration.ConfigurationManager.AppSettings["URL_Cancel"];
                    rq.Url = url;
               
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    invoice.TimeSent = DateTime.Now;
                    string rp = "";
                    rp = rq.Execute(jsonInvoiceCancel, "POST", "EinvoiceCancel", false, "", ref check);
                    if (check)
                    {
                        invoice.InvoiceStatus = Common.CommonConstants.INVOICECANCEL;
                        invoice.InvoiceDescription = Common.CommonConstants.CANCEL;
                        invoice.IsCancel = true;
                        invoice.CancelDateTime = DateTime.Now;
                        _iHermesInvoiceService.Update(invoice);
                        _iHermesInvoiceService.Save();

                        message = "HỦY HÓA ĐƠN THÀNH CÔNG!";
                        return Json(new { Type = messageType, Message = message, Title = "Thông báo" }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        message = rp;
                        messageType = Utils.DisplayMessage.TypeError;
                        return Json(new { Type = messageType, Message = message, Title = "Thông báo" }, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    message = "Có lỗi xảy ra";
                    messageType = Utils.DisplayMessage.TypeError;
                    return Json(new { Type = messageType, Message = message, Title = "Thông báo" }, JsonRequestBehavior.AllowGet);
                }
               
            }
            catch (Exception ex)
            {
                messageType = Utils.DisplayMessage.TypeError;
                Log.WriteLog(ex.ToString() + invoiceIsn, "InvoiceCancel.txt");
                message = ex.ToString();
                return Json(new { Type = messageType, Message = message, Title = "Thông báo" }, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult GetAll()
        {
            if (Session[CommonConstants.SessionAdmin] == null)
                Session[CommonConstants.SessionAdmin] = new List<OrderDetailViewModel>();
            var cart = (List<OrderDetailViewModel>)Session[CommonConstants.SessionAdmin];
            return Json(new
            {
                data = cart,
                status = true
            }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult OpenDoc()
        {
            string link = Request["link"].Trim();
            string invoiceins = Request["invoiceins"].Trim();
            string fileName = invoiceins + ".rtf";

            string filePath = Server.MapPath("~/invoiceHermes/") + fileName;
            FileInfo fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
            {
                System.IO.File.Copy(link, Server.MapPath("~/invoiceHermes/") + fileName);
            }
            if (!System.IO.File.Exists(filePath))
            {
                return HttpNotFound();
            }

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            var response = new FileContentResult(fileBytes, "application/octet-stream")
            {
                FileDownloadName = "invoiceH5.rtf"
            };
            return response;

            //return Json(new { Type = messageType, Message = message, Title = "Thông báo" }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Reprint()
        {
            string message = string.Empty;
            string messageType = Utils.DisplayMessage.TypeSuccess;
            try
            {
                string invoiceIsn = Request["invoiceins"].Trim();
                HermesInvoice invoice = _iHermesInvoiceService.GetByInvoiceIsn(invoiceIsn);
                var print = _iPrintService.GetByIsn(invoiceIsn);
                string filePath = "";
                bool checkFileExist = false;
                if (print != null)
                {
                    filePath = print.PrintLink;
                    FileInfo fileInfo = new FileInfo(filePath);
                    if (fileInfo.Exists)
                        checkFileExist = true;

                }
                if (!checkFileExist || invoice.InvoiceStatus == 3)
                {
                    
                    Utils.HttpRequest rqPdf = new Utils.HttpRequest();
                    rqPdf.Credentials = new Credentials()
                    {
                        UserName = "0106232917.ALS2",
                        Password = "57ab6d30"
                    };
                    string urlPdf = "https://api.einvoice.fpt.com.vn/search-invoice?stax=0106232917&sid=" + invoiceIsn + "&type=pdf";
                    rqPdf.Url = urlPdf;
                    bool check = false;
                    string rp = rqPdf.Execute(null, "GET", "EinvoicePDF", false, "", ref check);
                    var jArray = JArray.Parse(rp);
                    string obj = jArray[0]["pdf"].ToString();
                    string objBase64 = obj.Replace("data:application/pdf;base64,", "");
                    if (invoice.InvoiceStatus == 3)
                    {
                        filePath = CancelPdf(invoiceIsn, objBase64);
                    }
                    else
                    {
                        filePath = CreatePdf(invoiceIsn, objBase64);
                    }
                }

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                var response = new FileContentResult(fileBytes, "application/octet-stream")
                {
                    FileDownloadName = "eInvoice.pdf"
                };
                ActionLog actionLog = new ActionLog();
                actionLog.ActionType = "DOWNLOAD";
                actionLog.InvoiceID = invoiceIsn;
                actionLog.UserName = WebMatrix.WebData.WebSecurity.CurrentUserName;
                actionLog.UserID = WebMatrix.WebData.WebSecurity.CurrentUserId;
                actionLog.Created = DateTime.Now;
                actionLog.ActionName = "User " + actionLog.UserName + " DOWNLOAD FILE PDF";
                _iActionLogService.Add(actionLog);
                _iActionLogService.Save();
                return response;
            }
            catch (Exception ex)
            {
                 messageType = Utils.DisplayMessage.TypeError;
                Log.WriteLog(ex.ToString(), "InvoiceEdit.txt");
                message = ex.ToString();
                return Json(new { Type = messageType, Message = message, Title = "Thông báo" }, JsonRequestBehavior.AllowGet);
            }
           
        }
        public ActionResult CheckError()
        {
            string invoiceIsn = Request["invoiceIsn"].Trim();
            ViewBag.Message = _iResponseMessageService.GetByInvoiceIsn(invoiceIsn).ErrorMessageField;
            return View();
        }
        public static string CreatePdf(string invoiceIsn, string objBase64)
        {
            string year = DateTime.Now.Year.ToString();
            string month = DateTime.Now.ToString("yyyy-MM");
            string day = DateTime.Now.Day.ToString();
            string folderPath = @"C://EInvoice/" + year + "/" + month + "/" + day;
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            string filename = invoiceIsn + ".pdf";
            string filePath = Path.Combine(folderPath, filename);
            FileInfo fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
            {
                byte[] bytes = Convert.FromBase64String(objBase64);
                System.IO.FileStream stream =
             new FileStream(filePath, FileMode.CreateNew);
                System.IO.BinaryWriter writer =
                    new BinaryWriter(stream);
                writer.Write(bytes, 0, bytes.Length);
                writer.Close();
            }
            return filePath;

        }
        public static string CancelPdf(string invoiceIsn, string objBase64)
        {
            string year = DateTime.Now.Year.ToString();
            string month = DateTime.Now.ToString("yyyy-MM");
            string day = DateTime.Now.Day.ToString();
            string folderPath = @"C://EInvoice/" + year + "/" + month + "/" + day;
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            string filename = invoiceIsn + "Cancel.pdf";
            string filePath = Path.Combine(folderPath, filename);
            FileInfo fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
            {
                byte[] bytes = Convert.FromBase64String(objBase64);
                System.IO.FileStream stream =
             new FileStream(filePath, FileMode.CreateNew);
                System.IO.BinaryWriter writer =
                    new BinaryWriter(stream);
                writer.Write(bytes, 0, bytes.Length);
                writer.Close();
            }
            return filePath;
        }
        public static string GetLogFilePath(DateTime dateTime, string separateName)
        {
            if (separateName == null) separateName = "";
            string year = dateTime.Year.ToString();
            string month = dateTime.ToString("yyyy-MM");
            string date = dateTime.Date.ToString();
            string fileName = dateTime.ToString("yyyy-MM-dd") + "_" + separateName + ".txt";
            string folderPath = "";
            folderPath = Path.Combine("~/App_Data", year, month,date);
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
            string filePath = Path.Combine(folderPath, fileName);
            return filePath;
        }
        public string JsonInvoiceALSC(HermesInvoice invoice)
        {
            string jsonResult = "";
            List<HermesInvoiceDetail> listInvoiceDetail = new List<HermesInvoiceDetail>();
            listInvoiceDetail = _iHermesInvoiceDetailService.GetByInvoiceIsn(invoice.InvoiceIsn).ToList();
            if (listInvoiceDetail.Count > 0)
            {
                Account account = new Account();
                account.username = System.Configuration.ConfigurationManager.AppSettings["UserNameEInvoiceALSC"];
                account.password = System.Configuration.ConfigurationManager.AppSettings["PasswordEInvoiceALSC"];
                invoice.TimeSent = DateTime.Now;
                int line = 0;
                List<item> listItem = new List<item>();
                foreach (var invoiceDetail in listInvoiceDetail)
                {
                    line++;
                    item objItem = new item();
                    objItem.line = line;
                    objItem.name = invoiceDetail.Item;
                    objItem.quantity = invoiceDetail.Quantity;
                    objItem.price = invoiceDetail.UnitPrice;
                    objItem.amount = invoiceDetail.Amount;
                    objItem.unit = invoiceDetail.Unit;
                    objItem.type = "";
                    objItem.vrt = invoiceDetail.TaxRate;
                    objItem.c0 = invoice.Hawb.Trim().Length > 0 ? invoice.AWB + "/ " + invoice.Hawb : invoice.AWB + "/";
                    listItem.Add(objItem);
                }
                InvoiceFields invoiceField = new InvoiceFields();
                invoiceField.sid = invoice.InvoiceIsn;
                invoiceField.idt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                invoiceField.type = System.Configuration.ConfigurationManager.AppSettings["InvoiceFieldTypeALSC"];
                invoiceField.form = System.Configuration.ConfigurationManager.AppSettings["InvoiceFieldFormALSC"];
                invoiceField.serial = System.Configuration.ConfigurationManager.AppSettings["InvoiceFieldSerialALSC_IMPORT"];
                invoiceField.aun = 2;
                invoiceField.bname = invoice.KundName;
                invoiceField.buyer = invoice.KundName;
                if (string.IsNullOrEmpty(invoice.TaxCode.Trim()))
                {
                    invoiceField.btax = "";
                }
                else
                {
                    if (invoice.TaxCode.Length > 10)
                    {
                        invoiceField.btax = invoice.TaxCode.Trim().Replace("-", "").Replace(" ", "").Insert(10, "-");
                    }
                    else
                    {
                        invoiceField.btax = invoice.TaxCode;
                    }

                }
                invoiceField.baddr = invoice.Address.Trim().TrimEnd(',').Trim().TrimEnd(',').TrimStart(',');
                invoiceField.btel = "";
                //invoiceField.bmail = string.IsNullOrEmpty(invoice.Email.Trim())? "" : invoice.Email.Replace(" ","").Replace(',',';');
                invoiceField.bmail = "hddt.als@als.com.vn";
                invoiceField.paym = invoice.Payment == "CASH" ? "TM" : "CK";
                invoiceField.curr = "VND";
                invoiceField.exrt = 1;
                invoiceField.bacc = "";
                invoiceField.bbank = "";
                invoiceField.note = "Create eInvoice";
                invoiceField.sumv = (double)invoice.InvoiceTotalNoVatAmount;
                invoiceField.sum = (double)invoice.InvoiceTotalNoVatAmount;
                invoiceField.vatv = (double)invoice.InvoiceTotalVatAmount;
                invoiceField.vat = (double)invoice.InvoiceTotalVatAmount;
                invoiceField.totalv = (double)(invoice.InvoiceTotalAmount);
                invoiceField.word = "";
                invoiceField.discount = "";
                invoiceField.total = (double)(invoice.InvoiceTotalAmount);
                invoiceField.items = listItem;
                invoiceField.stax = System.Configuration.ConfigurationManager.AppSettings["StaxALSC"];
                InvoiceJson invoiceJson = new InvoiceJson();
                invoiceJson.user = account;
                invoiceJson.inv = invoiceField;
                jsonResult = JsonConvert.SerializeObject(invoiceJson);
            }

            return jsonResult;
        }
    }
}
