﻿using cn.jpush.api.common;
using cn.jpush.api.util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cn.jpush.api.push
{
    internal class PushClient:BaseHttpClient
    {
        private const String HOST_NAME_SSL = "https://api.jpush.cn";
        private const String HOST_NAME = "http://api.jpush.cn:8800";
        private const String PUSH_PATH = "/v2/push";

        private String appKey;
        private String masterSecret;
        private bool enableSSL = false;
        private long timeToLive;
        private bool apnsProduction = false;
        private HashSet<DeviceEnum> devices = new HashSet<DeviceEnum>();

        public PushClient(String masterSecret, String appKey, long timeToLive, HashSet<DeviceEnum> devices, bool apnsProduction)
        {
            this.appKey = appKey;
            this.masterSecret = masterSecret;
            this.timeToLive = timeToLive;
            this.devices = devices;
            this.apnsProduction = apnsProduction;
        }

        public MessageResult sendNotification(String notificationContent, NotificationParams notParams, String extras)
        {
            if (extras != null && extras != "")
            {
                notParams.NotyfyMsgContent.n_extras = extras;
            }
            notParams.NotyfyMsgContent.n_content = System.Web.HttpUtility.UrlEncode(notificationContent, Encoding.UTF8);
            //notParams.NotyfyMsgContent.n_content = notificationContent;
            return sendMessage(notParams, MsgTypeEnum.NOTIFICATIFY);
        }

        public MessageResult sendCustomMessage(String msgTitle, String msgContent, CustomMessageParams cParams, String extras)
        {
            if (msgTitle != null)
            {
                //cParams.CustomMsgContent.title = msgTitle;
                cParams.CustomMsgContent.title = System.Web.HttpUtility.UrlEncode(msgTitle, Encoding.UTF8);
            }
            if (extras != null && extras != "")
            {
                cParams.CustomMsgContent.extras = extras;
            }

            cParams.CustomMsgContent.message = System.Web.HttpUtility.UrlEncode(msgContent, Encoding.UTF8);
            return sendMessage(cParams, MsgTypeEnum.COUSTOM_MESSAGE);
        }


        private MessageResult sendMessage(MessageParams msgParams, MsgTypeEnum msgType) 
        {
            msgParams.ApnsProduction = this.apnsProduction ? 1 : 0;
            msgParams.AppKey = this.appKey;
            msgParams.MasterSecret = this.masterSecret;
            if (msgParams.TimeToLive == MessageParams.NO_TIME_TO_LIVE) 
            {
                msgParams.TimeToLive = this.timeToLive;            
            }
            if (this.devices != null)
            {
                foreach (DeviceEnum device in this.devices)
                {
                    msgParams.addPlatform(device);
                }
            }
            return sendPush(msgParams, msgType); 
        }

        private MessageResult sendPush(MessageParams msgParams, MsgTypeEnum msgType) 
        { 
            String url = enableSSL ? HOST_NAME_SSL : HOST_NAME;
            url += PUSH_PATH;
            String pamrams = prase(msgParams, msgType);
            //Console.WriteLine("begin post");
            ResponseResult result = sendPost(url, null, pamrams);
            //Console.WriteLine("end post");

            MessageResult messResult = new MessageResult();
            if (result.responseCode == System.Net.HttpStatusCode.OK)
            {
                //Console.WriteLine("responseContent===" + result.responseContent);
                messResult = (MessageResult)JsonTool.JsonToObject(result.responseContent, messResult);
                String content = result.responseContent;
            }
            messResult.ResponseResult = result;

            return messResult;

        }

        private String prase(MessageParams message, MsgTypeEnum msgType) 
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(message.SendNo).Append((int)message.ReceiverType).Append(message.ReceiverValue).Append(message.MasterSecret);
            String verificationCode = sb.ToString();
            //Console.WriteLine(verificationCode);
            verificationCode = Md5.getMD5Hash(verificationCode);
            sb.Clear();
            message.setMsgContent();
            String receiverVallue = System.Web.HttpUtility.UrlEncode(message.ReceiverValue, Encoding.UTF8);
            sb.Append("sendno=").Append(message.SendNo).Append("&app_key=").Append(message.AppKey).Append("&receiver_type=").Append((int)message.ReceiverType)
                .Append("&receiver_value=").Append(receiverVallue).Append("&verification_code=").Append(verificationCode)
                .Append("&msg_type=").Append((int)msgType).Append("&msg_content=").Append(message.MsgContent).Append("&platform=").Append(message.getPlatform())
                .Append("&apns_production=").Append(message.ApnsProduction);
            if(message.TimeToLive >= 0)
            {
                sb.Append("&time_to_live=").Append(message.TimeToLive);
            }
            if(message.OverrideMsgId != null)
            {
                sb.Append("&override_msg_id=").Append(message.OverrideMsgId);
            }
            //Console.WriteLine(sb.ToString());
            //Debug.Print(sb.ToString());
            return sb.ToString();
        }

    }

    enum MsgTypeEnum
    {
        NOTIFICATIFY = 1,
        COUSTOM_MESSAGE =2
    }
}
