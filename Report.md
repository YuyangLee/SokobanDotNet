# 人工智能基础 课程项目 1 - 推石头 实验报告

## 游戏实现

我们首先实现一个推石头的游戏。游戏基于 .NET 6 开发，需要 [安装 ASP.NET 运行时](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)。

### 运行

通过命令行运行 `SokobanDotNet.exe` 即可启动游戏。对于其他平台，可以在 `Codebase/SokobanDotNet/SokobanDotNet` 目录下编译：

```shell
dotnet restore && dotnet build
```

### 基本操作

#### 加载关卡

游戏启动后，通过路径指定关卡。留空进入随机生成模式（见下文）。

![Load from file](Assets/LoadFromFile.png)

关卡文件应该是多行的文本文件，格式如下

```
000000
002210
000110
014110
014100
081100
000000
```

数码采用 Flags Enum，即用每个二进制位表示一种格子

| 数码 $n$ | 0         | 1         | 2         | 3         | 4         |
| -------- | --------- | --------- | --------- | --------- | --------- |
| $2^n$    | 1         | 2         | 4         | 8         | 16        |
| 二进制   | `0b00000` | `0b00010` | `0b00100` | `0b01000` | `0b10000` |
| 格子类型 | 障碍      | 地面      | 洞口      | 石头      | 玩家      |

> 按位或操作 `|` 可以叠加两个格子，按位与 `&` 可以用于比较两个格子。
>
> 地图至少为 3 行 3 列，最外围必须是 `1`，石头和洞的数量必须一致。

#### 游戏操作

进入关卡后可以看到画面，"口"代表洞口，`o` 代表石头，"你"代表玩家。

<img src="Assets/GameUI.png" alt="Game UI" style="zoom:50%;" />

通过方向键移动，`Q` 退出游戏，`R` 重置游戏，`S` 启动搜索算法**从当前状态搜索解（可以进行一些步骤的游玩后开始搜索）**。将石头推入洞口后该位置的"口"变为"回"。将所有石头推入洞口即可获胜。

<img src="Assets/GameWon.png" alt="Game won" style="zoom:50%;" />

#### 手动关卡设计

打开 `Data/Maps/Template.xlsx` 可通过 Excel 设计关卡，数值同上表，颜色会被自动应用。

<img src="E:\Dev\SokobanDotNet\Assets\Map.Design.png" alt="Map design" style="zoom:50%;" />

设计完成后，保存为 CSV 文件，并使用 `csv2txt.py` 转为游戏程序可以读入的关卡数据文档：

```shell
python csv2txt.py --in_file PATH_TO_CSV_FILE
```

文件会被保存在同目录下的同名 TXT 文档中。启动游戏时加载该文件即可。

#### 自动关卡设计

在输入路径时留空进入随机地图生成模式。按照要求分别输入地图的尺寸、石头的数量即可。

![Generate](Assets\Generate.png)

由于并没有简易的显式算法生成地图，我们采用随机生成并通过搜索确定可解性的方式建图。将地图最外围设置成墙壁后，我们从起始状态开始，采用下面的转移概率，依次设置格子：

| 上一状态 | 砖头 | 地面 |
| -------- | ---- | ---- |
| 起始     | 0.5  | 0.5  |
| 砖头     | 0.4  | 0.6  |
| 地面     | 0.4  | 0.6  |

对于每个生成的地图，程序为之搜索解。程序会自动排除无法求解的问题、求解超过 20 min 的问题。接受一个解后，开始游戏：

![Generated](Assets\Generated.png)

此时可以按 S 开始搜索并执行答案：

![Generated.Searched](Assets\Generated.Searched.png)

## 问题建模（Baseline）

我们首先给出一个比较简单的建模。

- 状态 $s$ ：$H \times W$ 的场景表格
  - 包含 $N$ 个石头，位置为 $(h^{(i)}_s, w^{(i)}_s)$；
  - 包含 $N$ 个洞口，位置为 $(h^{(i)}_h, w^{(i)}_h)$；
  - 每一格内可能有不同的值：障碍物、空地、洞；
  - 空地、洞还可以叠加人、箱子（例如空地 + 箱子）。
- 动作 $a$：向上/下/左/右移动
- 状态转移：$(s_t, a_t) \to s_{t+1}$，根据游戏规则，可能出现
  - 人不动
  - 人移动
  - 人推动箱子
- 目标状态：$s_T$，其中所有洞口都有一个石头（不一一对应或一一对应）

### 问题求解

针对这个场景，可以使用 A* 算法搜索。

#### Goal-Manhattan-Distance as $h(s)$

可以使用距离目标的 Manhattan Distance 作为启发函数。对于石头、洞口一一对应的情景，只需计算对应石头与洞口的 Manhattan Distance 并求和
$$
h(s) = \sum_{i=1}^N \left( \vert h^{(i)}_s - h^{(i)}_h \vert + \vert w^{(i)}_s - w^{(i)}_h \vert \right)
$$
对于不一一对应的情景，计算所有配对可能中最小的 Manhattan Distance，需要求取 $A_N^N$ 次
$$
h(s) = \min_\pi \sum_{i=1}^N \left( \vert h^{(i)}_s - h^{(\pi(i))}_h \vert + \vert w^{(i)}_s - w^{(\pi(i))}_h \vert \right)
$$
#### Greedy-Manhattan-Distance as $h(s)$

为了避免计算 $A_N^N$ 次 Manhattan Distance，可以采用贪婪的 Manhattan Distance：对每个石头到达最近洞口的 Manhattan Distance 求和即可。

#### Estimated Geodesic Distance as $h(s)$

可以采用近似的 Path Distance 作为启发函数。预先计算好场景中不含石头时，任意一点到最近（某个）洞口的路径距离；在搜索时采用该距离可以获得更好的代价估计。



容易证明，上述启发函数都是可采纳的。

采用如下的 A* 算法即可进行搜索：

1. 初始化状态 $s_0$、$g(s_0) = 0$、优先队列 $Q$、闭节点列表 $C$
2. $s_0$ 加入 $Q$，优先级系数为 $h(s_0) + g(s_0)$
3. 当 $Q$ 不为空时
   1. 从 $Q$ 取出 $s$
   2. $s$ 加入 $C$
   3. 如果 $s$ 达成了目标
      1. 计算 $s_0 \to s$ 的动作序列 $\mathbf{s} = [ a_0, \dots, a_T]$
      2. 返回 $\mathbf{s}$
   4. 列举 $s$ 的可行操作集 $a_s$
   5. 对所有 $a \in a_s$
      1. 对 $s$ 执行 $a$ 得到 $s_a$
      2. 如果 $s_a \in C$ 且 $s_a$ 的 cost 比 $C$ 中的匹配项更高，则跳过此 $a$
      3. $g(s_a) = g(s) + 1$
      4. $s_a$ 加入 $Q$，优先级系数为 $h(s_a) + g(s_a)$
4. 返回 $\empty$

然而，实验表明这样的解法效率很低。实际上，已经有工作指出推箱子是一个 PSpace-Complete 的问题 ([Culberson, 1997](http://webdocs.cs.ualberta.ca/~joe/Preprints/Sokoban/paper.html))，这比 NP-Hard 问题更难解决。在实验中，一个观察是游戏的空间过大，操作角色在空间中逐步移动而不推动箱子会导致巨量的价值相等的待搜索节点，导致 reward 非常 sparse。

基于此考虑，我们改变上面对游戏的建模和算法，实现一种更高效的搜索方案。

## 问题建模

- 状态 $s$：同 baseline
- 动作 $a$：移动到石头 $s_i$ 的上/下/左/右邻格，并推动箱子
- 状态转移：$(s_t, a_t) \to s_{t+1}$，只允许合理的动作，即从起始位置到目标位置存在一条通路
- 目标状态：同 baseline

采用和 baseline 一样的 A* 算法求解。

### 算法加速

#### 剪枝

剪枝是搜索算法的重要加速方法。在本次项目中，我们剪去：

- 石头卡死：
  - 石头没有到达洞口，且其任意相邻两条边对应邻格（如上、右）均为障碍物
    - 依次判断四组相邻位置即可
  - 石头没有到达洞口，且其位于一个凹形障碍物区域边缘，无法从障碍物侧推出，并因此永远无法到达目标
    - 可以提前根据地图计算“死区”，剪去有石头进入死区的结点

#### 闭节点集合

随着搜索进行，闭节点集合将会迅速增长，判断子状态是否处于其中需要遍历整个列表，此过程逐渐变慢。可以通过使用 PriorityQueue 和 HashTable 来加速闭节点集合的搜索。本次项目中，我们采用 `Dictionary<int, List<SokobanGame>>` 为不同 Hash 值得游戏维护各自的列表。给定石头坐标 $(x_i, y_i)$、玩家坐标 $(x, y)$，游戏Hash 值计算方法为：
$$
H = \sum_{i=1}^{N_s}2^4 x_i + 2^4x + y_i + y
$$

## 样例结果

在测试样例上，采用 Release 模式编译可执行程序，在 Core i7-10875H （2.30GHz） 平台上搜索用时1308s，答案为 87 步骤。
