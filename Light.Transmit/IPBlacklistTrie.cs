using System.Net;

namespace Light.Transmit
{


    public class IPBlacklistTrie
    {
        private class TrieNode
        {

            public TrieNode[] Children = new TrieNode[31];


            //public Dictionary<int, TrieNode> Children { get; } = new();
            public bool IsEndOfSegment { get; set; }
        }



        private readonly TrieNode _root = new();

        // 添加 IP 段到前缀树
        public void Add(string cidr)
        {
            var parts = cidr.Split('/');
            var ipBytes = IPAddress.Parse(parts[0]).GetAddressBytes();
            int prefixLength = parts.Length == 2 ? int.Parse(parts[1]) : 32;
            var currentNode = _root;
            for (int i = 0; i < prefixLength; i++)
            {
                int bit = GetBit(ipBytes, i);
                if (currentNode.Children[bit] == null)
                {
                    currentNode.Children[bit] = new TrieNode();
                }
                currentNode = currentNode.Children[bit];
            }

            // 标记为 IP 段末尾
            currentNode.IsEndOfSegment = true;
        }

        // 检查 IP 是否在黑名单中
        public bool IsBlocked(IPAddress ip)
        {
            var ipBytes = ip.GetAddressBytes();
            var currentNode = _root;

            for (int i = 0; i < ipBytes.Length * 8; i++)
            {
                int bit = GetBit(ipBytes, i);
                if (currentNode.Children[bit] != null)
                {
                    currentNode = currentNode.Children[bit];
                    if (currentNode.IsEndOfSegment) return true; // 已匹配
                }
                else
                {
                    break;
                }
            }

            return false;
        }

        // 获取第 n 位（从左到右）
        private int GetBit(byte[] bytes, int bitIndex)
        {
            int byteIndex = bitIndex / 8;
            int bitOffset = 7 - (bitIndex % 8); // 从左到右读取
            return (bytes[byteIndex] >> bitOffset) & 1;
        }
    }

}
