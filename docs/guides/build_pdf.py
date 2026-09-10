import os
import re
import subprocess
import markdown

GITHUB_REPO_URL = "https://github.com/dpcks9677/Teserra/blob/main"

HTML_TEMPLATE = """<!DOCTYPE html>
<html lang="ko">
<head>
    <meta charset="UTF-8">
    <title>{title}</title>
    <!-- GitHub Markdown CSS -->
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/github-markdown-css/5.5.1/github-markdown-light.min.css">
    <!-- Pretendard Font -->
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/gh/orioncactus/pretendard/dist/web/static/pretendard.css">
    <!-- KaTeX -->
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/katex@0.16.9/dist/katex.min.css">
    <script defer src="https://cdn.jsdelivr.net/npm/katex@0.16.9/dist/katex.min.js"></script>
    <script defer src="https://cdn.jsdelivr.net/npm/katex@0.16.9/dist/contrib/auto-render.min.js"></script>
    <!-- Mermaid -->
    <script src="https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js"></script>
    <style>
        @page {
            size: A4;
            margin: 15mm 15mm 15mm 15mm;
            @bottom-right {
                content: counter(page);
            }
        }
        body {
            box-sizing: border-box;
            min-width: 200px;
            max-width: 900px;
            margin: 0 auto;
            padding: 20px 30px;
            font-family: "Pretendard", -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
            color: #24292f;
            background-color: #ffffff;
        }
        .markdown-body {
            font-size: 13.5px;
            line-height: 1.65;
        }
        .markdown-body h1 {
            font-size: 22px;
            border-bottom: 2px solid #eaecef;
            padding-bottom: 8px;
            margin-top: 24px;
            page-break-after: avoid;
        }
        .markdown-body h2 {
            font-size: 18px;
            border-bottom: 1px solid #eaecef;
            padding-bottom: 6px;
            margin-top: 20px;
            page-break-after: avoid;
        }
        .markdown-body h3 {
            font-size: 15px;
            margin-top: 16px;
            page-break-after: avoid;
        }
        .markdown-body table {
            font-size: 12px;
            width: 100%;
            border-collapse: collapse;
            margin: 14px 0;
            page-break-inside: auto;
        }
        .markdown-body tr {
            page-break-inside: avoid;
            page-break-after: auto;
        }
        .markdown-body th, .markdown-body td {
            padding: 6px 10px;
            border: 1px solid #d0d7de;
        }
        .markdown-body th {
            background-color: #f6f8fa;
        }
        .markdown-body pre {
            background-color: #f6f8fa;
            border-radius: 6px;
            padding: 12px;
            font-size: 12px;
            line-height: 1.45;
            page-break-inside: avoid;
        }
        .markdown-body code {
            font-family: "Consolas", "Courier New", monospace;
        }
        .markdown-body blockquote {
            border-left: 4px solid #0969da;
            color: #57606a;
            padding: 6px 14px;
            background-color: #f6f8fa;
            border-radius: 0 6px 6px 0;
            margin: 12px 0;
            page-break-inside: avoid;
        }
        .mermaid {
            display: flex;
            justify-content: center;
            margin: 16px 0;
            page-break-inside: avoid;
            background: #ffffff;
        }
        a {
            color: #0969da;
            text-decoration: none;
        }
        a:hover {
            text-decoration: underline;
        }
    </style>
</head>
<body class="markdown-body">
{content}

<script>
    // Mermaid 다이어그램 변환
    document.querySelectorAll('pre code.language-mermaid').forEach(function(el) {
        var div = document.createElement('div');
        div.className = 'mermaid';
        div.textContent = el.textContent;
        el.parentNode.parentNode.replaceChild(div, el.parentNode);
    });
    mermaid.initialize({ startOnLoad: true, theme: 'neutral', securityLevel: 'loose' });

    // KaTeX 수식 변환
    document.addEventListener("DOMContentLoaded", function() {
        if (typeof renderMathInElement !== 'undefined') {
            renderMathInElement(document.body, {
                delimiters: [
                    {left: '$$', right: '$$', display: true},
                    {left: '$', right: '$', display: false}
                ]
            });
        }
    });
</script>
</body>
</html>
"""

def convert_links(html_content):
    # .md 가이드 링크는 .pdf 로 매핑 (같은 디렉토리 내 PDF 간 이동)
    html_content = re.sub(r'href="augments_specification_and_status\.md"', r'href="augments_specification_and_status.pdf"', html_content)
    html_content = re.sub(r'href="architecture_overview\.md"', r'href="architecture_overview.pdf"', html_content)
    
    # 상대 코드 링크 및 상위 문서는 GitHub 웹 URL로 변환하여 모바일/iOS PDF 뷰어에서도 즉시 열리도록 처리
    html_content = re.sub(r'href="\.\./\.\./Assets/', f'href="{GITHUB_REPO_URL}/Assets/', html_content)
    html_content = re.sub(r'href="\.\./([a-zA-Z0-9_\-]+\.md)"', f'href="{GITHUB_REPO_URL}/docs/\\1"', html_content)
    return html_content

def process_file(md_path, pdf_path):
    print(f"변환 중: {md_path} -> {pdf_path}")
    with open(md_path, "r", encoding="utf-8") as f:
        text = f.read()

    # Markdown 변환
    html_body = markdown.markdown(
        text,
        extensions=['tables', 'fenced_code', 'toc', 'nl2br', 'sane_lists']
    )
    
    # 링크 변환
    html_body = convert_links(html_body)
    
    title = os.path.basename(md_path).replace(".md", "")
    full_html = HTML_TEMPLATE.replace("{title}", title).replace("{content}", html_body)

    temp_html = md_path.replace(".md", "_temp.html")
    with open(temp_html, "w", encoding="utf-8") as f:
        f.write(full_html)

    # Edge 실행 파일 경로
    edge_paths = [
        r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
        r"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
        r"C:\Program Files\Google\Chrome\Application\chrome.exe"
    ]
    browser = None
    for p in edge_paths:
        if os.path.exists(p):
            browser = p
            break

    if not browser:
        print("브라우저 실행 파일을 찾을 수 없습니다.")
        return

    abs_html = os.path.abspath(temp_html)
    abs_pdf = os.path.abspath(pdf_path)

    cmd = [
        browser,
        "--headless=new",
        "--disable-gpu",
        "--allow-file-access-from-files",
        "--run-all-compositor-stages-before-draw",
        "--virtual-time-budget=6000",
        f"--print-to-pdf={abs_pdf}",
        f"file:///{abs_html}"
    ]
    subprocess.run(cmd, check=True)
    
    if os.path.exists(temp_html):
        os.remove(temp_html)
    print(f"생성 완료: {pdf_path} (크기: {os.path.getsize(abs_pdf):,} bytes)")

if __name__ == "__main__":
    base_dir = r"docs\guides"
    files = [
        ("architecture_overview.md", "architecture_overview.pdf"),
        ("augments_specification_and_status.md", "augments_specification_and_status.pdf")
    ]
    for md, pdf in files:
        md_p = os.path.join(base_dir, md)
        pdf_p = os.path.join(base_dir, pdf)
        process_file(md_p, pdf_p)
