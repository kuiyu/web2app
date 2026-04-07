import{_ as y,c as r,l as n,b as l,e as g,U as a,F as h,m as u,Q as c,a as o,y as m,t as p,T as _,O as A,a8 as f,a9 as T,x as I}from"./index-CsnCTIdg.js";const w={class:"guide-layout"},C={class:"guide-sidebar"},D={class:"guide-header"},P={class:"guide-title"},x={class:"guide-nav"},B=["onClick"],Q={class:"guide-content"},b={class:"content-title"},F=["innerHTML"],M={__name:"Guide",setup(S){const e=I("quickstart"),d=[{key:"quickstart",title:"快速开始",icon:"Promotion",content:`
      <h3>注册和登录</h3>
      <ol>
        <li><strong>访问系统</strong>：打开浏览器，访问系统部署地址（例如：<a href="http://localhost:3000" target="_blank">http://localhost:3000</a>）</li>
        <li><strong>注册账号</strong>：点击"注册"按钮，填写用户名、密码等信息，完成注册后自动登录</li>
        <li><strong>登录系统</strong>：如果已有账号，输入用户名和密码登录，登录成功后进入主界面</li>
      </ol>
      <h3>主界面介绍</h3>
      <p>系统主界面包含以下主要功能模块：</p>
      <ul>
        <li><strong>工作流</strong>：创建和管理 AI 工作流</li>
        <li><strong>Agent</strong>：构建和配置 AI Agent</li>
        <li><strong>知识库</strong>：管理文档和知识库</li>
        <li><strong>提示词模板</strong>：管理和使用提示词模板</li>
        <li><strong>数据分析</strong>：查看使用统计和数据报表</li>
      </ul>
    `},{key:"workflow",title:"工作流",icon:"Share",content:`
      <h3>什么是工作流</h3>
      <p>工作流是一种将多个 AI 处理步骤串联起来的自动化流程，可以实现复杂的业务逻辑处理。</p>
      <h3>创建工作流</h3>
      <ol>
        <li>点击顶部导航栏的 <strong>工作流</strong> 菜单</li>
        <li>点击右上角 <strong>+ 新建工作流</strong> 按钮</li>
        <li>在弹出的编辑器中拖拽节点，连接流程</li>
        <li>配置每个节点的参数，点击 <strong>保存</strong></li>
      </ol>
      <h3>导入工作流</h3>
      <p>支持导入 JSON 格式的工作流文件，点击 <strong>导入工作流</strong> 按钮，选择本地文件即可。</p>
      <h3>发布工作流</h3>
      <p>工作流创建完成后，点击 <strong>发布</strong> 按钮即可上线，发布后可在 Agent 中引用。</p>
    `},{key:"agent",title:"Agent",icon:"Service",content:`
      <h3>什么是 Agent</h3>
      <p>Agent 是一个具备特定能力的 AI 助手，可以通过配置系统提示词、工具和知识库来定制其行为。</p>
      <h3>创建 Agent</h3>
      <ol>
        <li>进入 <strong>Agent 管理</strong> 页面</li>
        <li>点击 <strong>+ 新建 Agent</strong></li>
        <li>填写名称、描述、选择图标</li>
        <li>配置默认 AI 模型和系统提示词</li>
        <li>按需关联工具和知识库</li>
        <li>点击 <strong>确定</strong> 完成创建</li>
      </ol>
      <h3>关键配置说明</h3>
      <ul>
        <li><strong>系统提示词</strong>：定义 Agent 的角色和行为规范，是最重要的配置项</li>
        <li><strong>可用工具</strong>：Agent 可以调用的外部工具，如 HTTP 请求、数据库查询等</li>
        <li><strong>关联知识库</strong>：Agent 回答时会自动检索关联知识库中的内容</li>
        <li><strong>温度参数</strong>：控制回答的创造性，0 最保守，1 最有创意</li>
      </ul>
    `},{key:"knowledge",title:"知识库",icon:"Collection",content:`
      <h3>知识库简介</h3>
      <p>知识库用于存储和管理文档，Agent 可以通过检索知识库来获取相关信息，提升回答准确性。</p>
      <h3>创建知识库</h3>
      <ol>
        <li>进入 <strong>知识库</strong> 页面，点击 <strong>新建知识库</strong></li>
        <li>填写名称，选择嵌入模型（必填）</li>
        <li>配置切片大小和重叠大小</li>
        <li>选择默认检索策略（推荐混合检索）</li>
        <li>点击 <strong>创建</strong></li>
      </ol>
      <h3>上传文档</h3>
      <p>进入知识库详情页，点击右上角上传按钮，支持 PDF、Word、TXT、Markdown 等格式。</p>
      <h3>查看分片</h3>
      <p>文档处理完成后，可点击 <strong>查看分片</strong> 查看文档被切分的内容，支持搜索和编辑分片内容。</p>
      <h3>检索策略说明</h3>
      <ul>
        <li><strong>混合检索</strong>（推荐）：结合向量检索和全文检索，效果最佳</li>
        <li><strong>向量检索</strong>：基于语义相似度检索，适合语义理解场景</li>
        <li><strong>全文检索</strong>：基于关键词匹配，适合精确查找场景</li>
      </ul>
    `},{key:"prompt",title:"提示词模板",icon:"EditPen",content:`
      <h3>提示词模板简介</h3>
      <p>提示词模板是预设的系统提示词，可以在创建 Agent 时快速引用，提升配置效率。</p>
      <h3>创建模板</h3>
      <ol>
        <li>进入 <strong>配置 → 提示词模板</strong></li>
        <li>点击 <strong>新建模板</strong></li>
        <li>填写模板名称和提示词内容</li>
        <li>支持使用变量，格式为 <code>{{变量名}}</code></li>
      </ol>
      <h3>使用模板</h3>
      <p>在创建或编辑 Agent 时，系统提示词输入框支持从模板库中选择并填充内容。</p>
    `},{key:"faq",title:"常见问题",icon:"QuestionFilled",content:`
      <h3>Q：Agent 回答不准确怎么办？</h3>
      <p>A：可以从以下几个方面优化：</p>
      <ul>
        <li>优化系统提示词，明确 Agent 的角色和回答规范</li>
        <li>关联相关知识库，提供准确的背景知识</li>
        <li>调整温度参数，降低随机性</li>
        <li>更换更强大的 AI 模型</li>
      </ul>
      <h3>Q：知识库文档处理失败怎么办？</h3>
      <p>A：请检查以下几点：</p>
      <ul>
        <li>确认文件格式是否支持（PDF、Word、TXT、Markdown）</li>
        <li>确认嵌入模型配置是否正确，API Key 是否有效</li>
        <li>文件大小不超过 50MB</li>
      </ul>
      <h3>Q：如何提升检索效果？</h3>
      <p>A：建议使用混合检索策略，并配置重排序模型。同时适当调整切片大小（推荐 500-1000 字符）和重叠大小（推荐 50-100 字符）。</p>
      <h3>Q：模型调用失败怎么排查？</h3>
      <p>A：进入 <strong>配置 → 调试实验室</strong> 查看执行日志，确认 API Key 和 Base URL 配置正确，网络连接正常。</p>
    `}];return(L,i)=>{const k=c("Document"),s=c("el-icon"),v=c("el-divider");return o(),r("div",w,[n("aside",C,[n("div",D,[n("div",P,[l(s,null,{default:g(()=>[l(k)]),_:1}),i[0]||(i[0]=a(" 用户使用指南",-1))]),i[1]||(i[1]=n("div",{class:"guide-sub"},"完整操作手册",-1))]),n("nav",x,[(o(),r(h,null,u(d,t=>n("div",{key:t.key,class:m(["nav-item",{active:e.value===t.key}]),onClick:N=>e.value=t.key},[l(s,null,{default:g(()=>[(o(),p(_(t.icon)))]),_:2},1024),a(" "+A(t.title),1)],10,B)),64))])]),n("main",Q,[(o(),r(h,null,u(d,t=>f(n("div",{key:t.key},[n("div",b,[l(s,null,{default:g(()=>[(o(),p(_(t.icon)))]),_:2},1024),a(" "+A(t.title),1)]),l(v),n("div",{class:"content-body",innerHTML:t.content},null,8,F)]),[[T,e.value===t.key]])),64))])])}}},H=y(M,[["__scopeId","data-v-e37de56d"]]);export{H as default};
