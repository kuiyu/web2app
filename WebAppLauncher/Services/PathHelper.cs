/*
 * 名称：web应用容器
 * 功能：用程序打开本地网页，vue页面，网站
 * 作者微信：runsoft1024
 */
using System;
using System.IO;

namespace WebAppLauncher.Services
{
    public static class PathHelper
    {
        /// <summary>
        /// 判断给定的路径是否为网址
        /// </summary>
        /// <param name="path">要检查的路径</param>
        /// <returns>如果是网址返回true，否则返回false</returns>
        public static bool IsUrl(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            // 去除前后空格
            path = path.Trim();

            // 检查是否以常见的网址协议开头
            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("file://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("ws://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // 检查协议相对URL（以//开头）
            if (path.StartsWith("//"))
                return true;

            // 检查是否包含://（其他协议）
            if (path.Contains("://", StringComparison.Ordinal))
                return true;

            return false;
        }

        /// <summary>
        /// 根据路径类型构建合适的Uri
        /// </summary>
        /// <param name="basePath">基础路径（对于本地文件）</param>
        /// <param name="relativePath">相对路径或网址</param>
        /// <returns>对应的Uri对象</returns>
        public static Uri BuildUri(string basePath, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("路径不能为空", nameof(relativePath));

            relativePath = relativePath.Trim();

            // 如果是网址，直接创建Uri
            if (IsUrl(relativePath))
            {
                // 确保网址有正确的协议
                if (relativePath.StartsWith("//"))
                {
                    relativePath = "https:" + relativePath;
                }
                return new Uri(relativePath);
            }

            // 否则，作为本地文件处理
            string fullPath;
            if (Path.IsPathRooted(relativePath))
            {
                // 如果是绝对路径，直接使用
                fullPath = relativePath;
            }
            else
            {
                // 如果是相对路径，与基础路径组合
                fullPath = Path.Combine(basePath, relativePath);
            }

            // 确保路径存在
            if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
            {
                throw new FileNotFoundException($"文件或目录不存在: {fullPath}", fullPath);
            }

            // 创建file:// Uri
            return new Uri(fullPath);
        }

        /// <summary>
        /// 检查路径是否可用（文件存在或为有效网址）
        /// </summary>
        /// <param name="basePath">基础路径（对于本地文件）</param>
        /// <param name="relativePath">相对路径或网址</param>
        /// <returns>如果路径可用返回true，否则返回false</returns>
        public static bool IsPathAvailable(string basePath, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return false;

            relativePath = relativePath.Trim();

            // 如果是网址，假设总是可用（网络可达性需要实际访问才能确定）
            if (IsUrl(relativePath))
                return true;

            // 检查本地文件
            try
            {
                string fullPath;
                if (Path.IsPathRooted(relativePath))
                {
                    fullPath = relativePath;
                }
                else
                {
                    fullPath = Path.Combine(basePath, relativePath);
                }

                return File.Exists(fullPath) || Directory.Exists(fullPath);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取路径的显示名称
        /// </summary>
        /// <param name="path">路径或网址</param>
        /// <returns>显示名称</returns>
        public static string GetDisplayName(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "未知路径";

            path = path.Trim();

            if (IsUrl(path))
            {
                try
                {
                    var uri = new Uri(path);
                    return $"{uri.Host} ({uri.Scheme}://...)";
                }
                catch
                {
                    return "网址";
                }
            }

            return Path.GetFileName(path) ?? path;
        }
    }
}