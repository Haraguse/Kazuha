import sys
try:
    from qfluentwidgets import FluentIcon
    print("FluentIcon dir:", dir(FluentIcon), file=sys.stderr)
    for item in FluentIcon:
        print(item.name, file=sys.stderr)
except Exception as e:
    print(f"Error: {e}", file=sys.stderr)
