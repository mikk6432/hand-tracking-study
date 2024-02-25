import pandas as pd
import matplotlib.pyplot as plt

def plot_csv(csv_file):
    # Load the CSV data into a DataFrame
    data = pd.read_csv(csv_file)

    # Plotting
    plt.figure(figsize=(10, 6))
    plt.plot(data['Time'], data['Distance'], marker='o')
    plt.title('Distance Over Time')
    plt.xlabel('Time')
    plt.ylabel('Distance')
    plt.grid(True)
    plt.show()

if __name__ == "__main__":
    import sys
    csv_file = sys.argv[1] if len(sys.argv) > 1 else './Hand-tracking-evaluation/distance_data.csv'
    plot_csv(csv_file)
