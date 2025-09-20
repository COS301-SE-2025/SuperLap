#!/usr/bin/env python3
"""
Unity EditMode Test Coverage Calculator
Calculates average coverage specifically for EditMode_* test files.
Can be used locally or in GitHub workflows.
"""

import json
import os
import sys
from pathlib import Path

def calculate_editmode_coverage(coverage_file_path):
    """
    Calcul    # Exit with appropriate code
    if not success:
        sys.exit(1)
    
    # Set GitHub Actions outputs if in GitHub environment
    if format_type == 'github' and coverage_data.get('editmode_tests_found', False):
        # Write to GitHub Actions outputs
        github_output = os.getenv('GITHUB_OUTPUT')
        if github_output:
            with open(github_output, 'a') as f:
                f.write(f"line_coverage={coverage_data['overall_line_coverage']}\n")
                f.write(f"method_coverage={coverage_data['overall_method_coverage']}\n")
                f.write(f"test_count={coverage_data['test_count']}\n")
    
    # No exit code based on coverage since no threshold requiredge metrics for EditMode tests and specific MainAssembly classes.
    
    Args:
        coverage_file_path (str): Path to the Summary.json file
        
    Returns:
        dict: Coverage metrics for selected test classes
    """
    try:
        with open(coverage_file_path, 'r') as f:
            data = json.load(f)
        
        # Define the classes we want to include in coverage calculation
        target_classes = {
            'EditMode': [  # All EditMode tests
                'APIManagerIntegrationTest',
                'APIManagerMockTest', 
                'APITest',
                'PSOFileTests'
            ],
            'MainAssembly': [  # Specific MainAssembly classes
                'Particle',
                'RacelineOptimizer.RacelineExporter',
                'RacelineOptimizer.SavitzkyGolayFilter',
                'CornerDetector',
                'EdgeData',
                'RacelineOptimizer.TrackSampler',
                'RacelineOptimizer.PSO',
                'RacelineOptimizer.PSOInterface'
            ]
        }
        
        all_test_classes = []
        total_covered_lines = 0
        total_coverable_lines = 0
        total_covered_methods = 0
        total_methods = 0
        
        # Process each assembly
        for assembly in data['coverage']['assemblies']:
            assembly_name = assembly['name']
            
            if assembly_name in target_classes:
                target_class_names = target_classes[assembly_name]
                
                # Find matching classes in this assembly
                for test_class in assembly['classesinassembly']:
                    class_name = test_class['name']
                    
                    if class_name in target_class_names:
                        # Calculate coverage for this class
                        class_line_coverage = (test_class['coveredlines'] / test_class['coverablelines'] * 100) if test_class['coverablelines'] > 0 else 0
                        class_method_coverage = (test_class['coveredmethods'] / test_class['totalmethods'] * 100) if test_class['totalmethods'] > 0 else 0
                        
                        all_test_classes.append({
                            'name': class_name,
                            'assembly': assembly_name,
                            'line_coverage': round(class_line_coverage, 1),
                            'method_coverage': round(class_method_coverage, 1),
                            'covered_lines': test_class['coveredlines'],
                            'total_lines': test_class['coverablelines'],
                            'covered_methods': test_class['coveredmethods'],
                            'total_methods': test_class['totalmethods']
                        })
                        
                        # Add to totals
                        total_covered_lines += test_class['coveredlines']
                        total_coverable_lines += test_class['coverablelines']
                        total_covered_methods += test_class['coveredmethods']
                        total_methods += test_class['totalmethods']
        
        if not all_test_classes:
            return {
                'error': 'No target test classes found in coverage data',
                'editmode_tests_found': False
            }
        
        # Calculate overall metrics
        overall_line_coverage = (total_covered_lines / total_coverable_lines * 100) if total_coverable_lines > 0 else 0
        overall_method_coverage = (total_covered_methods / total_methods * 100) if total_methods > 0 else 0
        
        return {
            'editmode_tests_found': True,
            'overall_line_coverage': round(overall_line_coverage, 1),
            'overall_method_coverage': round(overall_method_coverage, 1),
            'total_covered_lines': total_covered_lines,
            'total_coverable_lines': total_coverable_lines,
            'total_covered_methods': total_covered_methods,
            'total_methods': total_methods,
            'test_count': len(all_test_classes),
            'test_details': all_test_classes,
            'summary': {
                'excellent': sum(1 for t in all_test_classes if t['line_coverage'] >= 90),
                'good': sum(1 for t in all_test_classes if 70 <= t['line_coverage'] < 90),
                'needs_improvement': sum(1 for t in all_test_classes if t['line_coverage'] < 70)
            },
            'by_assembly': {
                'EditMode': [t for t in all_test_classes if t['assembly'] == 'EditMode'],
                'MainAssembly': [t for t in all_test_classes if t['assembly'] == 'MainAssembly']
            }
        }
        
    except FileNotFoundError:
        return {
            'error': f'Coverage file not found: {coverage_file_path}',
            'editmode_tests_found': False
        }
    except json.JSONDecodeError:
        return {
            'error': f'Invalid JSON in coverage file: {coverage_file_path}',
            'editmode_tests_found': False
        }
    except Exception as e:
        return {
            'error': f'Error reading coverage data: {str(e)}',
            'editmode_tests_found': False
        }

def print_coverage_report(coverage_data, format_type='console'):
    """
    Print coverage report in different formats.
    
    Args:
        coverage_data (dict): Coverage data from calculate_editmode_coverage
        format_type (str): 'console', 'github', or 'json'
    """
    
    if not coverage_data.get('editmode_tests_found', False):
        if format_type == 'json':
            print(json.dumps(coverage_data, indent=2))
        else:
            print(f"❌ Error: {coverage_data.get('error', 'Unknown error')}")
        return False
    
    if format_type == 'json':
        print(json.dumps(coverage_data, indent=2))
        return True
    
    # Console and GitHub formats
    print()
    print("🧪 Unity Test Coverage Report (Selected Classes)")
    print("=" * 50)
    
    # Overall summary
    line_cov = coverage_data['overall_line_coverage']
    method_cov = coverage_data['overall_method_coverage']
    
    print(f"📊 Overall Coverage:")
    print(f"   Lines: {line_cov}% ({coverage_data['total_covered_lines']}/{coverage_data['total_coverable_lines']})")
    print(f"   Methods: {method_cov}% ({coverage_data['total_covered_methods']}/{coverage_data['total_methods']})")
    print(f"   Total Classes: {coverage_data['test_count']}")
    
    # Show breakdown by assembly
    by_assembly = coverage_data.get('by_assembly', {})
    if by_assembly:
        editmode_count = len(by_assembly.get('EditMode', []))
        mainassembly_count = len(by_assembly.get('MainAssembly', []))
        print(f"   EditMode Tests: {editmode_count}")
        print(f"   MainAssembly Classes: {mainassembly_count}")
    
    # Coverage quality indicator
    if line_cov >= 90:
        quality = "🟢 Excellent"
    elif line_cov >= 70:
        quality = "🟡 Good"
    else:
        quality = "🔴 Needs Improvement"
    
    print(f"   Quality: {quality}")
    
    # Group by assembly for display
    if by_assembly:
        for assembly_name, classes in by_assembly.items():
            if classes:
                print(f"\n📋 {assembly_name} Classes:")
                print("-" * 40)
                
                for test in classes:
                    name = test['name']
                    line_pct = test['line_coverage']
                    method_pct = test['method_coverage']
                    
                    # Status emoji based on line coverage
                    if line_pct >= 90:
                        status = "🟢"
                    elif line_pct >= 70:
                        status = "🟡"
                    else:
                        status = "🔴"
                    
                    print(f"{status} {name}")
                    print(f"   Lines: {line_pct}% ({test['covered_lines']}/{test['total_lines']})")
                    print(f"   Methods: {method_pct}% ({test['covered_methods']}/{test['total_methods']})")
    else:
        # Fallback to flat list if no assembly grouping
        print(f"\n📋 All Test Classes:")
        print("-" * 40)
        
        for test in coverage_data['test_details']:
            name = test['name']
            line_pct = test['line_coverage']
            method_pct = test['method_coverage']
            
            # Status emoji based on line coverage
            if line_pct >= 90:
                status = "🟢"
            elif line_pct >= 70:
                status = "🟡"
            else:
                status = "🔴"
            
            print(f"{status} {name}")
            print(f"   Lines: {line_pct}% ({test['covered_lines']}/{test['total_lines']})")
            print(f"   Methods: {method_pct}% ({test['covered_methods']}/{test['total_methods']})")
    
    # Summary statistics
    summary = coverage_data['summary']
    print(f"\n📈 Summary:")
    print(f"   🟢 Excellent (≥90%): {summary['excellent']} classes")
    print(f"   🟡 Good (70-89%): {summary['good']} classes")
    print(f"   🔴 Needs work (<70%): {summary['needs_improvement']} classes")
    
    # GitHub workflow output (using newer syntax)
    if format_type == 'github':
        print(f"\nline_coverage={line_cov}")
        print(f"method_coverage={method_cov}")
        print(f"test_count={coverage_data['test_count']}")
        print(f"quality={quality}")
        # For GitHub Actions step outputs
        print(f"::notice title=Unity Test Coverage::Line Coverage: {line_cov}%, Method Coverage: {method_cov}%")
    
    print()
    return True

def main():
    """Main function for command line usage."""
    
    # Parse command line arguments
    format_type = 'console'
    coverage_file = None
    
    if len(sys.argv) > 1:
        if sys.argv[1] in ['--json', '-j']:
            format_type = 'json'
        elif sys.argv[1] in ['--github', '-g']:
            format_type = 'github'
        elif sys.argv[1] in ['--help', '-h']:
            print("Unity EditMode Coverage Calculator")
            print()
            print("Usage:")
            print("  python calculate-coverage.py [options] [coverage_file]")
            print()
            print("Description:")
            print("  Calculates coverage for EditMode tests and selected MainAssembly classes:")
            print("  - All EditMode test classes")
            print("  - Particle, CornerDetector, EdgeData")  
            print("  - RacelineOptimizer.* classes (PSO, PSOInterface, etc.)")
            print()
            print("Options:")
            print("  -j, --json     Output as JSON")
            print("  -g, --github   Output for GitHub workflows")
            print("  -h, --help     Show this help")
            print()
            print("Arguments:")
            print("  coverage_file  Path to Summary.json (default: Unity/CodeCoverage/Report/Summary.json)")
            print()
            print("Examples:")
            print("  python calculate-coverage.py")
            print("  python calculate-coverage.py --json")
            print("  python calculate-coverage.py /path/to/Summary.json")
            return
        else:
            coverage_file = sys.argv[1]
    
    if len(sys.argv) > 2:
        coverage_file = sys.argv[2]
    
    # Default coverage file path
    if not coverage_file:
        # Try to find the coverage file relative to script location
        script_dir = Path(__file__).parent
        project_root = script_dir.parent
        coverage_file = project_root / "Unity" / "CodeCoverage" / "Report" / "Summary.json"
    
    coverage_file = Path(coverage_file)
    
    if not coverage_file.exists():
        print(f"❌ Coverage file not found: {coverage_file}")
        print()
        print("💡 Run Unity tests first to generate coverage:")
        print("   python scripts/unity.py")
        print()
        print("💡 Or specify a different coverage file:")
        print(f"   python calculate-coverage.py /path/to/Summary.json")
        sys.exit(1)
    
    # Calculate and display coverage
    coverage_data = calculate_editmode_coverage(str(coverage_file))
    success = print_coverage_report(coverage_data, format_type)
    
    # Exit with appropriate code
    if not success:
        sys.exit(1)
    
    # Exit with error if coverage is below threshold (for CI)
    if coverage_data.get('editmode_tests_found', False):
        line_coverage = coverage_data['overall_line_coverage']
        if format_type == 'github' and line_coverage < 70:
            print(f"::error::Test coverage {line_coverage}% is below 70% threshold")
            sys.exit(1)

if __name__ == "__main__":
    main()
