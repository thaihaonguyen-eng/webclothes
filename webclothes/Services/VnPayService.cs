using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace webclothes.Services
{
    public class VnPayService
    {
        private readonly IConfiguration _config;
        public VnPayService(IConfiguration config)
        {
            _config = config;
        }

        public string CreatePaymentUrl(HttpContext context, webclothes.Models.Order order)
        {
            // Sandbox test credentials from VNPay
            string vnp_TmnCode = _config["VnPay:TmnCode"];
            string vnp_HashSecret = _config["VnPay:HashSecret"];
            string vnp_Url = _config["VnPay:Url"];
            string vnp_Returnurl = $"{context.Request.Scheme}://{context.Request.Host}/Cart/PaymentCallback";

            var vnpayData = new SortedList<string, string>();
            vnpayData.Add("vnp_Version", "2.1.0");
            vnpayData.Add("vnp_Command", "pay");
            vnpayData.Add("vnp_TmnCode", vnp_TmnCode);
            vnpayData.Add("vnp_Amount", ((long)order.TotalAmount * 100).ToString()); // Amount must be multiplied by 100
            vnpayData.Add("vnp_CreateDate", order.OrderDate.ToString("yyyyMMddHHmmss"));
            vnpayData.Add("vnp_CurrCode", "VND");
            vnpayData.Add("vnp_IpAddr", context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
            vnpayData.Add("vnp_Locale", "vn");
            vnpayData.Add("vnp_OrderInfo", "Thanh toan Đơn hàng:" + order.Id);
            vnpayData.Add("vnp_OrderType", "other");
            vnpayData.Add("vnp_ReturnUrl", vnp_Returnurl);
            vnpayData.Add("vnp_TxnRef", order.Id.ToString() + "_" + DateTime.Now.Ticks); 

            // Build query
            var query = new StringBuilder();
            foreach (var kv in vnpayData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    query.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }
            string queryString = query.ToString().TrimEnd('&');

            // Hmac request data
            string signData = queryString;
            byte[] hashBytes;
            using (var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(vnp_HashSecret)))
            {
                hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signData));
            }
            string vnp_SecureHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

            string paymentUrl = vnp_Url + "?" + queryString + "&vnp_SecureHash=" + vnp_SecureHash;

            return paymentUrl;
        }

        public bool ValidateSignature(string rsprawurl, string inputHash, string hashSecret)
        {
            if (string.IsNullOrEmpty(hashSecret)) {
                hashSecret = _config["VnPay:HashSecret"];
            }
            
            byte[] hashBytes;
            using (var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(hashSecret)))
            {
                hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(rsprawurl));
            }
            string myChecksum = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
