using System;
using System.Collections.Generic;
using System.Globalization;

namespace TesterClient.Api
{
    public class LoadRequest : IApiRequest
    {
        /// <summary>
        /// Поле profile
        /// </summary>
        public int Profile;
        /// <summary>
        /// Возможные виды поля Controller
        /// </summary>
        public enum Controllers {acc, gir}
        /// <summary>
        /// Поле data
        /// </summary>
        public Dictionary<string, double> Data;
        /// <summary>
        /// Поле conroler
        /// </summary>
        public Controllers Controller;
        /// <summary>
        /// Инициализация запроса Load
        /// </summary>
        /// <param name="profile">profile</param>
        /// <param name="controller">controler</param>
        public LoadRequest(int profile, Controllers controller)
        {
            Profile = profile;
            Controller = controller;
            Data=new Dictionary<string, double>();
        }
        /// <summary>
        /// Добавить новую запись в поле data
        /// </summary>
        /// <param name="datetime"></param>
        /// <param name="value"></param>
        public void AddData(DateTime datetime, double value)
        {
            string strDate = string.Format("{0:yyyy-MM-dd HH:mm:ss.fff}", datetime);
            Data.Add(strDate, value);
        }
        /// <summary>
        /// Получить подготовленный для отправки на сервер запрос
        /// </summary>
        /// <returns>Тело запроса</returns>
        public string Raw()
        {
            
            string data = string.Empty;
            foreach (KeyValuePair<string,double> pair in Data)
            {
                data += string.Format("[\"{0}\", {1}],", pair.Key, pair.Value.ToString(CultureInfo.CurrentCulture).Replace(',', '.'));
            }
            if (data == string.Empty)
                data = "[]";
            else
                data=data.Remove(data.Length - 1, 1);
            data = "[" + data + "]";
            string formatted = "'profile': {0}, 'controler': '{1}', 'data':{2}".Replace('\'', '"');
            return "{"+string.Format(formatted, Profile.ToString(), Controller.ToString(), data)+"}";
        }
        /// <summary>
        /// Content-Type Load запроса
        /// </summary>
        /// <returns>application/json</returns>
        public string ContentType()
        {
            return "application/json";
        }
        
    }
}