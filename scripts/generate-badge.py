#!/usr/bin/env python3
"""
SVG Coverage Badge Generator
Generates an SVG badge file with coverage percentage for private repositories.
"""

import sys
import json
from pathlib import Path

def get_badge_color(coverage):
    """Get badge color based on coverage percentage"""
    if coverage >= 90:
        return "#4c1"  # brightgreen
    elif coverage >= 80:
        return "#97ca00"  # green
    elif coverage >= 70:
        return "#a4a61d"  # yellowgreen
    elif coverage >= 60:
        return "#dfb317"  # yellow
    elif coverage >= 50:
        return "#fe7d37"  # orange
    else:
        return "#e05d44"  # red

def generate_coverage_badge_svg(coverage_percentage, output_file):
    """Generate an SVG badge with coverage percentage"""
    
    # Round coverage to 1 decimal place
    coverage = round(float(coverage_percentage), 1)
    coverage_text = f"{coverage}%"
    
    # Get color based on coverage
    color = get_badge_color(coverage)
    
    # Calculate text widths (approximate)
    label_text = "coverage"
    label_width = len(label_text) * 6 + 10  # ~6px per char + padding
    coverage_width = len(coverage_text) * 6 + 10
    total_width = label_width + coverage_width
    
    # SVG template
    svg_template = f"""<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="{total_width}" height="20" role="img" aria-label="coverage: {coverage_text}">
    <title>coverage: {coverage_text}</title>
    <linearGradient id="s" x2="0" y2="100%">
        <stop offset="0" stop-color="#bbb" stop-opacity=".1"/>
        <stop offset="1" stop-opacity=".1"/>
    </linearGradient>
    <clipPath id="r">
        <rect width="{total_width}" height="20" rx="3" fill="#fff"/>
    </clipPath>
    <g clip-path="url(#r)">
        <rect width="{label_width}" height="20" fill="#555"/>
        <rect x="{label_width}" width="{coverage_width}" height="20" fill="{color}"/>
        <rect width="{total_width}" height="20" fill="url(#s)"/>
    </g>
    <g fill="#fff" text-anchor="middle" font-family="Verdana,Geneva,DejaVu Sans,sans-serif" text-rendering="geometricPrecision" font-size="110">
        <text aria-hidden="true" x="{label_width//2 * 10}" y="150" fill="#010101" fill-opacity=".3" transform="scale(.1)" textLength="{(len(label_text)*60)}">coverage</text>
        <text x="{label_width//2 * 10}" y="140" transform="scale(.1)" fill="#fff" textLength="{(len(label_text)*60)}">coverage</text>
        <text aria-hidden="true" x="{(label_width + coverage_width//2) * 10}" y="150" fill="#010101" fill-opacity=".3" transform="scale(.1)" textLength="{(len(coverage_text)*60)}">{coverage_text}</text>
        <text x="{(label_width + coverage_width//2) * 10}" y="140" transform="scale(.1)" fill="#fff" textLength="{(len(coverage_text)*60)}">{coverage_text}</text>
    </g>
</svg>"""

    # Write SVG to file
    with open(output_file, 'w') as f:
        f.write(svg_template)
    
    print(f"Generated coverage badge: {coverage_text} -> {output_file}")
    return True

def main():
    """Main function for command line usage"""
    if len(sys.argv) < 3:
        print("Usage: python generate-badge.py <coverage_percentage> <output_file>")
        print("Example: python generate-badge.py 87.2 docs/images/coverage-badge.svg")
        sys.exit(1)
    
    try:
        coverage = float(sys.argv[1])
        output_file = sys.argv[2]
        
        # Validate coverage range
        if coverage < 0 or coverage > 100:
            print("Error: Coverage percentage must be between 0 and 100")
            sys.exit(1)
        
        # Create output directory if it doesn't exist
        output_path = Path(output_file)
        output_path.parent.mkdir(parents=True, exist_ok=True)
        
        # Generate the badge
        generate_coverage_badge_svg(coverage, output_file)
        
    except ValueError:
        print("Error: Coverage percentage must be a valid number")
        sys.exit(1)
    except Exception as e:
        print(f"Error: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()
