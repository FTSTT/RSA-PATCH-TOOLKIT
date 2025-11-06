[English](README_EN.md)

# RSA 补丁工具箱 / RSA PATCH TOOLKIT

> 🧠 部分软件功能与本文档编写过程使用了 **AI 辅助**，以提升开发与说明效率。  
> 本工具用于 **半字节 / 1 字节 / 固定 CRC32 补丁辅助**，并不适合用于分解大数 —— 只是为了好玩。

---

## 🌟 特色 Features

- 支持 **HEX / Decimal / Base64 格式识别与转换**
- 提供 **微调字节模式** 与 **固定 CRC32 模式**
- 对 **字符串型与字节流型的 N** 都可尝试 **半字节替换**
- 可执行 **Dry Run 演练**、**RSA 加解密验证**、**Brent/ECM 因数分解**
- 自带 **掩码编辑器 (FrmMaskEditor)** 支持 `*` 可变位定义
-  **中英双语界面**
- 所有操作都有 **日志输出与彩色标记**

---

## 🧩 示例 Example

### 例子 / Example

| 类型 | 说明 | 示例 |
|:--|:--|:--|
| 原始 N | 初始 RSA 模数（绿色底色表示原始 N） | `duDe1rHc22OLeI9tElSwEIhKIx9X/VOEDWC2jGDo1iUitTWFaROy1KHuYRi/ruz19BZIUUE5xIUeL7tzVmCasufYKwzj2MWTzpZdHjDMoU4ow5o5oa2j2soUnSQn6K5VSfhGxxnhmtcwoBp3qiaM3p085zkdyye46Cx4jaS+nSk` |
| N1 | 半字节变换（最小差异） | `duDe1rHc22OLeI9tElSw**B**IhKIx9X/VOEDWC2jGDo1iUitTWFaROy1KHuYRi/ruz19BZIUUE5xIUeL7tzVmCasufYKwzj2MWTzpZdHjDMoU4ow5o5oa2j2soUnSQn6K5VSfhGxxnhmtcwoBp3qiaM3p085zkdyye46Cx4jaS+nSk` |
| N2 | 固定 CRC32 变换（CRC32 值不变） | `duDe1**I**Hc22OLeI9tElSwEIhKIx9X/VOEDWC2jGDo1iUitTWFaROy1**H**HuY**R**S/ruz19BZIUUE5xIUeL7tzVmCasuf**Q**Kwzj2MWTzpZdHjDMoU4ow5o5oa2j2soUnSQ**y**6K5VSfhGxxnhmtc**w**kBp3q**N**aM3p085zkdyye4**J**C**x**fjaS+nSk` |
| N3 | 其他演示变体 | `**keygenGeneratedBySomeCoolGuy**/VOEDWC2jGDo1iUit**M**WFaROy1**K**HkYRi/ru**0**19BZIUU**2**5xIUeL7t**D**cmCasu**1**YK**y**zj2MWTzpZdHjDM**P**U4**o**a5o5oa2j2soUnS**1**n6K5VSf**9**Gxxnhj**t**cwoBp3q**i**xM3p085zkdyye46Cx4jaS+nSk` |

> 以上差异字符在实际网页中以 **红色粗体** 标出，原始 N 背景为绿色（#ecfdf5）。

---

## 🚀 新手指南 / Quick Start

1. **粘贴 [P]** 导入 N，自动识别 Hex/Decimal/Base64 格式。  
2. 若识别错误，可点击 isHEX / isDecimal / isBase64 手动修正（不会触发转换）。  
3. 需要格式转换？直接切换格式下拉框（例：Hex → Base64）。  
4. 选择运行模式：  
   - **微调字节模式** ：设定“尝试替换位置”和“方向”，可选“按半字节”与“输入的 HEX 是字符串”；  
   - **固定 CRC32 模式** ：点击 “掩码” 定义 `*` 可变位，配置允许字符集与 Unicode 开关。  
5. 点击 **开始** 执行；候选结果将输出到日志窗口，差异以红色粗体标记。  

### 🔑 常用快捷键

| 功能 | 说明 |
|:--|:--|
| **翻转 [R]** | 仅对 Hex 生效，按字节序反转 |
| **Dry Run** | 演练模式，仅打印不计算 |
| **帮助** | 输出内置示例（带标记） |

---

## ⚙️ 选项说明 / Options

### 顶部输入区
- **TextN**：承载 N 的文本，支持 Hex / Decimal / Base64。  
- **ComboFormat**：切换显示与转换格式。  
- **粘贴 [P]**：清理空白并自动识别；Base64 自动补齐 `=`。  
- **翻转 [R]**：按字节整体反转。  
- **isHEX / isDecimal / isBase64 / isHex(lower)**：仅修改下拉框文本，不改变内容。  

### 中部选项区 · 微调字节模式
- **尝试替换位置**（1 基）  
- **方向**：仅当前 / 向左 / 向右  
- **按半字节**：对 nibble 高低 4 位微调（支持字符串与字节流型 N）  
- **输入的 HEX 是字符串**：按字符或按字节处理  

### 中部选项区 · 固定 CRC32 模式
- **掩码**：`*` 表示可变，其余字符冻结  
- **允许字符集**：Hex / Dec / Base64 或自定义  
- **Unicode**：ASCII 与 UTF-16LE 切换  
- **需要字符数**：可修改位数量  

### 通用选项
- **Dry Run**：仅记录流程，不计算  
- **Rsa Test**：对 N/D 随机加解密验证  
- **Brent 迭代次数**：Rho-Brent 算法上限  
- **小素数试除**：启用基础试除  
- **尝试小曲线**：ECM 小曲线数量  
- **结果数量下限**：最少生成条数  
- **结果记录日志**：将候选写入日志  
- **线程数**：设置并发度  
- **接受素数 N**：允许 N 为素数  

---

## ⚙️ 技术依赖

RSA PATCH TOOLKIT 在数学运算部分使用了两项外部动态链接库：

- **libgmp**：GNU 多精度算术库（GNU MP），用于高精度大整数运算。  
- **libecm**：Elliptic Curve Method (ECM) 分解库，用于小因子搜索与椭圆曲线分解。  

这些库均为开源组件，按各自的开源许可再分发。

## ⚖️ 版权 / License
本项目采用 **Apache License 2.0** 协议。

© 2025 RSA 补丁工具箱 / RSA PATCH TOOLKIT. All rights reserved.  
代码和文档允许非商业性学习与修改，转载请注明来源。
