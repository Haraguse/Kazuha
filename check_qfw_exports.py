import sys
try:
    import qfluentwidgets
    print("qfluentwidgets dir:", dir(qfluentwidgets), file=sys.stderr)
except Exception as e:
    print(f"Error: {e}", file=sys.stderr)
