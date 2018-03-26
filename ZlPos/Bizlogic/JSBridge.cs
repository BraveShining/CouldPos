﻿using System;
using System.Collections.Generic;
using System.Threading;
using CefSharp;
using CefSharp.WinForms;
using ZlPos.Dao;
using Newtonsoft.Json;
using ZlPos.Bean;
using ZlPos.Config;
using ZlPos.Models;
using ZlPos.Manager;
using log4net;
using System.Globalization;

namespace ZlPos.Bizlogic
{
    /// <summary>
    /// create by sVen 2018年3月15日： method invoke class
    /// </summary>
    class JSBridge
    {
        private static ILog logger = null;

        private ChromiumWebBrowser browser;

        private static LoginUserManager _LoginUserManager;

        static JSBridge instance = null;

        public static JSBridge Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new JSBridge();
                }
                return instance;
            }
        }

        private JSBridge()
        {

        }

        public ChromiumWebBrowser Browser { get => browser; set => browser = value; }


        /// <summary>
        /// native调用js 示例
        /// </summary>
        public void ExecuteScriptAsync()
        {
            browser.ExecuteScriptAsync("printInvokeJSMethod('hello world')");
        }


        /// <summary>
        /// js调用 native method 示例
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public string NativeUnityMethod(object state)
        {
            return "here is c# solution string>>>>>>>[" + state.ToString() + "]";
        }


        public string GetDeviceId()
        {
            return new Guid().ToString();
        }

        public string GetNetWorkStatus()
        {
            return "json";
        }

        #region Login
        /// <summary>
        /// 离线登陆
        /// </summary>
        /// <param name="json"></param>
        public void Login(string json)
        {
            ResponseEntity responseEntity = new ResponseEntity();
            if (string.IsNullOrEmpty(json))
            {
                responseEntity.Code = ResponseCode.Failed;
                responseEntity.Msg = "参数不能为空";

                //TODO这里考虑开个线程池去操作
                ThreadPool.QueueUserWorkItem(new WaitCallback(CallbackMethod), new object[] { "loginCallBack", responseEntity });
            }
            else
            {
                try
                {
                    // 将H5传过来的用户输入信息进行解析，用于离线登录匹配
                    LoginEntity loginEntity = JsonConvert.DeserializeObject<LoginEntity>(json);
                    DbManager dbManager = DBUtils.Instance.DbManager;
                    if (loginEntity != null)
                    {
                        using (var db = SugarDao.GetInstance())
                        {
                            List<UserEntity> userList;
                            if (String.IsNullOrEmpty(loginEntity.account) || loginEntity.account == "0")
                            {
                                userList = db.Queryable<UserEntity>().Where(it => it.account == loginEntity.account
                                                                            && it.password == loginEntity.password).ToList();
                            }
                            else
                            {
                                userList = db.Queryable<UserEntity>().Where(it => it.username == loginEntity.username
                                                                            && it.shopcode == loginEntity.shopcode
                                                                            && it.password == loginEntity.password).ToList();
                            }
                            if (userList != null && userList.Count == 1)
                            {
                                _LoginUserManager.Instance.Login = true;
                                _LoginUserManager.Instance.UserEntity = userList[0];
                                UserVM userVM = new UserVM();
                                UserEntity userEntity = userList[0];
                                userVM.user_info = userEntity;
                                List<ShopConfigEntity> configEntities = db.Queryable<ShopConfigEntity>().Where(
                                                                        it => it.id == int.Parse(userEntity.shopcode) + int.Parse(userEntity.branchcode)).ToList();
                                //TODO...先去写saveOrUpadteUserInfo 再来完成这边login的逻辑

                            }
                        }
                    }
                    //只是为了调试加的
                    ThreadPool.QueueUserWorkItem(new WaitCallback(CallbackMethod), new object[] { "loginCallBack", responseEntity });
                    System.Windows.Forms.MessageBox.Show("called loginCallBack js method!!!");
                }
                catch (Exception e)
                {

                }
            }
        }
        #endregion

        #region SaveOrUpdateUserInfo
        /// <summary>
        /// 保存或更新用户信息
        /// </summary>
        /// <param name="json"></param>
        public void SaveOrUpdateUserInfo(string json)
        {
            try
            {
                DbManager dbManager = DBUtils.Instance.DbManager;
                UserVM userVM = JsonConvert.DeserializeObject<UserVM>(json);
                if (userVM != null)
                {
                    logger.Info("保存或更新用户信息,获取到的userVM：" + userVM.ToString());
                    ShopConfigEntity config = userVM.config;
                    UserEntity user_info = userVM.user_info;
                    if (config != null && user_info != null)
                    {
                        config.id = int.Parse(user_info.branchcode) + int.Parse(user_info.shopcode);
                        dbManager.SaveOrUpdate(config);
                    }
                    if (user_info != null)
                    {
                        dbManager.SaveOrUpdate(user_info);

                        ContextCache.SetShopcode(user_info.shopcode);
                    }
                    _LoginUserManager.Instance.Login = true;
                    _LoginUserManager.Instance.UserEntity = user_info;

                    logger.Info("保存或更新用户信息接口：用户在线登陆并保存用户信息成功");

                }
                else
                {
                    logger.Info("存或更新用户信息接口：用户登录成功但用户信息保存失败");
                }
            }
            catch (Exception)
            {
                logger.Info("保存或更新用户信息接口：保存数据库操作异常");
                //throw;
            }
        }
        #endregion

        #region SaveOrUpdateCommodityInfo
        /// <summary>
        /// 保存或更新商品信息
        /// </summary>
        public ResponseEntity SaveOrUpdateCommodityInfo(string json)
        {
            ResponseEntity responseEntity = new ResponseEntity();
            if (_LoginUserManager.Instance.Login)
            {
                string shopcode = _LoginUserManager.Instance.UserEntity.shopcode;
                string branchcode = _LoginUserManager.Instance.UserEntity.branchcode;
                try
                {
                    DbManager dbManager = DBUtils.Instance.DbManager;
                    CommodityInfoVM commodityInfoVM = JsonConvert.DeserializeObject<CommodityInfoVM>(json);
                    if (commodityInfoVM != null)
                    {
                        commodityInfoVM.shopcode = shopcode;
                        commodityInfoVM.branchcode = branchcode;

                        List<CategoryEntity> categoryEntities = commodityInfoVM.categorys;
                        List<CommodityEntity> commoditys = commodityInfoVM.commoditys;
                        List<MemberEntity> memberEntities = commodityInfoVM.memberlevels;
                        List<PayTypeEntity> paytypes = commodityInfoVM.paytypes;
                        List<AssistantsEntity> assistants = commodityInfoVM.assistants;
                        List<CashierEntity> users = commodityInfoVM.users;
                        List<SupplierEntity> suppliers = commodityInfoVM.suppliers;
                        // add: 2018/2/27
                        List<BarCodeEntity> barCodes = commodityInfoVM.barcodes;
                        List<CommodityPriceEntity> commodityPriceEntityList = commodityInfoVM.commoditypricelist;

                        #region 循环saveorupdate 效率很慢 TODO...: 这里应该改造成bulksaveorupdate提高效率
                        //保存商品分类信息
                        if (categoryEntities != null)
                        {
                            foreach (CategoryEntity categoryEntity in categoryEntities)
                            {
                                dbManager.SaveOrUpdate(categoryEntity);
                            }
                        }
                        //保存商品信息
                        if (commoditys != null)
                        {
                            foreach (CommodityEntity commodityEntity in commoditys)
                            {
                                dbManager.SaveOrUpdate(commodityEntity);
                            }
                        }
                        //保存会员等级信息
                        if (memberEntities != null)
                        {
                            foreach (MemberEntity memberEntity in memberEntities)
                            {
                                dbManager.SaveOrUpdate(memberEntity);
                            }
                        }
                        //保存付款方式信息
                        if (paytypes != null)
                        {
                            foreach (PayTypeEntity payTypeEntity in paytypes)
                            {
                                dbManager.SaveOrUpdate(payTypeEntity);
                            }
                        }
                        //保存收银员信息
                        if (assistants != null)
                        {
                            foreach (AssistantsEntity assistantsEntity in assistants)
                            {
                                dbManager.SaveOrUpdate(assistantsEntity);
                            }
                        }
                        //保存收银员信息
                        if (users != null)
                        {
                            foreach (CashierEntity cashierEntity in users)
                            {
                                dbManager.SaveOrUpdate(cashierEntity);
                            }
                        }
                        //保存供应商信息
                        if (suppliers != null)
                        {
                            foreach (SupplierEntity supplierEntity in suppliers)
                            {
                                dbManager.SaveOrUpdate(supplierEntity);
                            }
                        }
                        // add: 2018/2/27
                        //保存条码表信息
                        if (barCodes != null)
                        {
                            foreach (BarCodeEntity barCodeEntity in barCodes)
                            {
                                //由shopcode+commoditycode做联合主键,防止跨商户商品数据的commoditycode相同
                                barCodeEntity.uid = shopcode + "_" + barCodeEntity.commoditycode;
                                barCodeEntity.shopcode = shopcode;
                                dbManager.SaveOrUpdate(barCodeEntity);
                            }
                        }
                        //保存调价表信息
                        if (commodityPriceEntityList != null)
                        {
                            foreach (CommodityPriceEntity commodityPriceEntity in commodityPriceEntityList)
                            {
                                dbManager.SaveOrUpdate(commodityPriceEntity);
                            }
                        }
                        #endregion

                        dbManager.SaveOrUpdate(commodityInfoVM);
                        logger.Info("保存和更新商品信息接口：信息保存成功");
                        responseEntity.Code = ResponseCode.SUCCESS;
                    }
                }
                catch (Exception e)
                {
                    logger.Info("保存和更新商品信息接口：" + e.StackTrace);
                    responseEntity.Code = ResponseCode.Failed;
                }
            }
            else
            {
                logger.Info("保存和更新商品信息接口：用户未登录");
                responseEntity.Code = ResponseCode.Failed;
            }
            logger.Info("数据保存成功");
            return responseEntity;

        }
        #endregion

        #region GetCommodityInfo
        /// <summary>
        /// 获取所有商品和分类信息
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public ResponseEntity GetCommodityInfo(string json)
        {
            ResponseEntity responseEntity = new ResponseEntity();
            DbManager dbManager = DBUtils.Instance.DbManager;
            if (_LoginUserManager.Instance.Login)
            {
                UserEntity userEntity = _LoginUserManager.Instance.UserEntity;
                try
                {
                    List<CommodityEntity> commodityEntities = null;
                    List<CategoryEntity> categoryEntities = null;
                    List<MemberEntity> memberEntities = null;
                    List<PayTypeEntity> payTypeEntities = null;
                    List<AssistantsEntity> assistantsEntities = null;
                    List<CashierEntity> cashierEntities = null;
                    List<SupplierEntity> supplierEntities = null;
                    List<CommodityInfoVM> commodityInfoVMList = null;
                    List<BarCodeEntity> barCodes = null;
                    List<CommodityPriceEntity> commodityPriceList = null;

                    using (var db = SugarDao.GetInstance())
                    {
                        // TODO: 2017/11/3 按固定精确度返回，
                        //固定精确到shopcode
                        supplierEntities = db.Queryable<SupplierEntity>().Where(it => it.shopcode == userEntity.shopcode).ToList();

                        payTypeEntities = db.Queryable<PayTypeEntity>().Where(it => it.shopcode == userEntity.shopcode).ToList();

                        commodityInfoVMList = db.Queryable<CommodityInfoVM>().Where(it => it.shopcode == userEntity.shopcode).ToList();

                        categoryEntities = db.Queryable<CategoryEntity>().Where(it => it.shopcode == userEntity.shopcode
                                                                            && it.del == "0").OrderBy(it => it.categorycode).ToList();

                        // add: 2018/2/27
                        barCodes = db.Queryable<BarCodeEntity>().Where(it => it.shopcode == userEntity.shopcode).ToList();

                        //固定精确到shopcode + branchcode
                        assistantsEntities = db.Queryable<AssistantsEntity>().Where(it => it.shopcode == userEntity.shopcode
                                                                                && it.branchcode == userEntity.branchcode).ToList();

                        cashierEntities = db.Queryable<CashierEntity>().Where(it => it.shopcode == userEntity.shopcode
                                                                            && it.branchcode == userEntity.branchcode).ToList();

                        commodityPriceList = db.Queryable<CommodityPriceEntity>().Where(it => it.shopcode == userEntity.shopcode
                                                                                    && it.branchcode == userEntity.branchcode).ToList();

                        //按 membermode 区分SSM和CSM
                        if ("CSM".Equals(userEntity.membermodel)) //跨店
                        {
                            memberEntities = db.Queryable<MemberEntity>().Where(it => it.shopcode == userEntity.shopcode).ToList();
                        }
                        else //单店
                        {
                            memberEntities = db.Queryable<MemberEntity>().Where(it => it.shopcode == userEntity.shopcode
                                                                            && it.branchcode == userEntity.branchcode).ToList();
                        }


                        CommodityInfoVM commodityInfoVM = new CommodityInfoVM();
                        if (commodityInfoVMList != null && commodityInfoVMList.Count > 0)
                        {
                            CommodityInfoVM infoVM = commodityInfoVMList[commodityInfoVMList.Count - 1];//如有多条，取最新的一条
                            commodityInfoVM = infoVM;
                        }
                        if (commodityEntities == null)
                        {
                            commodityEntities = new List<CommodityEntity>();
                        }
                        if (categoryEntities == null)
                        {
                            categoryEntities = new List<CategoryEntity>();
                        }
                        if (memberEntities == null)
                        {
                            memberEntities = new List<MemberEntity>();
                        }
                        if (payTypeEntities == null)
                        {
                            payTypeEntities = new List<PayTypeEntity>();
                        }
                        if (assistantsEntities == null)
                        {
                            assistantsEntities = new List<AssistantsEntity>();
                        }
                        if (cashierEntities == null)
                        {
                            cashierEntities = new List<CashierEntity>();
                        }
                        if (supplierEntities == null)
                        {
                            supplierEntities = new List<SupplierEntity>();
                        }
                        // add: 2018/2/27
                        if (barCodes == null)
                        {
                            barCodes = new List<BarCodeEntity>();
                        }
                        if (commodityPriceList == null)
                        {
                            commodityPriceList = new List<CommodityPriceEntity>();
                        }

                        commodityInfoVM.categorys = categoryEntities;
                        commodityInfoVM.commoditys = commodityEntities;
                        commodityInfoVM.memberlevels = memberEntities;
                        commodityInfoVM.paytypes = payTypeEntities;
                        commodityInfoVM.assistants = assistantsEntities;
                        commodityInfoVM.users = cashierEntities;
                        commodityInfoVM.suppliers = supplierEntities;
                        commodityInfoVM.barcodes = barCodes;
                        commodityInfoVM.commoditypricelist = commodityPriceList;
                        responseEntity.Data = commodityInfoVM;
                        responseEntity.Code = ResponseCode.SUCCESS;
                        responseEntity.Msg = "获取所有商品信息成功";


                    }
                }
                catch (Exception)
                {
                    responseEntity.Code = ResponseCode.Failed;
                    responseEntity.Msg = "数据异常";
                }
            }
            else
            {
                responseEntity.Code = ResponseCode.Failed;
                responseEntity.Msg = "用户未登陆";
            }

            return responseEntity;

        }
        #endregion

        #region GetLastRequestTime
        /// <summary>
        /// 获取最后更新时间
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public string GetLastRequestTime(string json)
        {
            String lastRequestTime = "";
            DbManager dbManager = DBUtils.Instance.DbManager;
            if (_LoginUserManager.Instance.Login)
            {
                try
                {
                    using (var db = SugarDao.GetInstance())
                    {
                        List<CommodityInfoVM> lastRequestTimeList = db.Queryable<CommodityInfoVM>()
                                                                    .Where(it => it.shopcode == _LoginUserManager.Instance.UserEntity.shopcode
                                                                    && it.branchcode == _LoginUserManager.Instance.UserEntity.branchcode).ToList();
                        if (lastRequestTimeList != null && lastRequestTimeList.Count > 0)
                        {
                            CommodityInfoVM commodityInfoVM = lastRequestTimeList[lastRequestTimeList.Count - 1];
                            if (commodityInfoVM != null)
                            {
                                lastRequestTime = commodityInfoVM.requesttime;
                            }
                        }

                    }
                }
                catch (Exception e)
                {
                    logger.Error(e.Message + ">>>" + e.StackTrace);
                }
            }
            else
            {

            }

            return lastRequestTime;
        }
        #endregion

        #region GetLastUserName
        /// <summary>
        /// 获取最后一次登录shopcode
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public string GetLastUserName(string json)
        {
            string shopcode = "";
            try
            {
                shopcode = ContextCache.GetShopcode();
            }
            catch (Exception e)
            {
                logger.Error(e.Message + e.StackTrace);
            }
            return shopcode;
        }
        #endregion

        #region SaveOneSaleBill
        /// <summary>
        /// 保存销售单据接口
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public ResponseEntity SaveOneSaleBill(string json)
        {
            ResponseEntity responseEntity = new ResponseEntity();

            if (string.IsNullOrEmpty(json))
            {
                logger.Info("保存销售单据接口：空字符串");
                responseEntity.Code = ResponseCode.Failed;
                responseEntity.Msg = "参数不能为空";
                return responseEntity;
            }
            DbManager dbManager = DBUtils.Instance.DbManager;

            BillEntity billEntity = JsonConvert.DeserializeObject<BillEntity>(json);
            if (billEntity == null)
            {
                logger.Info("保存销售单据接口：json解析失败");
                responseEntity.Code = ResponseCode.Failed;
                responseEntity.Msg = "参数格式错误";
                return responseEntity;
            }
            try
            {
                DateTimeFormatInfo dtFormat = new DateTimeFormatInfo();
                dtFormat.ShortDatePattern = "yyyy-MM-dd HH:mm:ss";
                DateTime insertDate = DateTime.MinValue;
                try
                {
                    insertDate = Convert.ToDateTime(billEntity.saletime, dtFormat);
                }
                catch (Exception e)
                {
                    logger.Error(e.Message + e.StackTrace);
                }
                if(insertDate == DateTime.MinValue)
                {
                    insertDate = DateTime.Now;
                }
                billEntity.insertTime = Utils.DateUtils.ConvertDataTimeToLong(insertDate);

                dbManager.SaveOrUpdate(billEntity);
            }
            catch (Exception e)
            {
                logger.Error("保存销售单据接口： 异常");
            }
            List<BillCommodityEntity> commoditys = billEntity.commoditys;
            List<PayDetailEntity> paydetails = billEntity.paydetails;
            if (commoditys == null || commoditys.Count == 0)
            {
                logger.Info("保存销售单据接口：该单据没有商品信息");
            }
            else
            {
                foreach (BillCommodityEntity billCommodityEntity in commoditys)
                {
                    try
                    {
                        billCommodityEntity.uid = billCommodityEntity
                                .ticketcode
                                + "_"
                                + billCommodityEntity.id;
                        dbManager.SaveOrUpdate(billCommodityEntity);
                    }
                    catch (Exception e)
                    {
                        logger.Error("保存销售单据接口：dbManager.saveOrUpdate(billCommodityEntity)--DbException");
                    }
                }
            }

            if (paydetails == null || paydetails.Count == 0)
            {
                logger.Info("保存销售单据接口：该单据没有付款方式信息");
            }
            else
            {
                foreach (PayDetailEntity payDetailEntity in paydetails)
                {
                    try
                    {
                        dbManager.SaveOrUpdate(payDetailEntity);
                    }
                    catch (Exception e)
                    {
                        logger.Error("保存销售单据接口： dbManager.saveOrUpdate(payDetailEntity)--DbException");
                    }
                }
            }
            responseEntity.Code = ResponseCode.SUCCESS;
            responseEntity.Msg = "保存单据成功";
            return responseEntity;
        }
        #endregion















        public void TestORM(string json)
        {
            Employee employees = JsonConvert.DeserializeObject<Employee>(json);

            try
            {

                DbManager dbManager = DBUtils.Instance.DbManager;

                dbManager.SaveOrUpdate(employees);
                using (var db = SugarDao.GetInstance())
                {
                    //db.CodeFirst.BackupTable().InitTables(typeof(Employee));
                    //db.DbMaintenance.IsAnyTable(typeof(Employee).Name);
                    //db.CodeFirst.InitTables(typeof(Employee));

                    //db.Insertable(employees).ExecuteReturnEntity();


                    //db.BeginTran();

                    //db.IsEnableAttributeMapping = true;

                    //no table ex
                    //db.SqlBulkCopy(new List<Employee>(){ employees });

                    //db.InsertOrUpdate(employees);

                    //db.CommitTran();
                }
            }
            catch (Exception ex)
            {
                string s = ex.StackTrace;
            }

            //System.Windows.Forms.MessageBox.Show("ok");
            return;
        }



        private void CallbackMethod(object state)
        {

            object[] paramsArr = (object[])state;
            //first params is method name  
            string methodName = paramsArr[0] as string;

            //real params tofix
            ResponseEntity responseEntity = paramsArr[1] as ResponseEntity;

            //模拟耗时操作
            Thread.Sleep(5000);


            browser.ExecuteScriptAsync(methodName + "('" + JsonConvert.SerializeObject(responseEntity) + "')");
        }
    }
}
