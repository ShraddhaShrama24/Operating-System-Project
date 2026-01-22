const algoDiv = document.getElementById("algos");
const currentPage = document.getElementById("currentPage");

// ✅ Connect to backend WebSocket
const ws = new WebSocket(`ws://localhost:5000/`);

// ✅ Chart.js setup
let chartData = {
    labels: [],  // time
    fifo: [],
    lru: [],
    clock: [],
    optimal: []
};

let chartRef = null;

function setupChart() {
    const ctx = document.getElementById('algoChart').getContext('2d');

    chartRef = new Chart(ctx, {
        type: 'line',
        data: {
            labels: chartData.labels,
            datasets: [
                {
                    label: 'FIFO Hit Ratio (%)',
                    borderColor: '#4A6CF7',
                    data: chartData.fifo,
                    fill: false,
                    tension: 0.2
                },
                {
                    label: 'LRU Hit Ratio (%)',
                    borderColor: '#2ECC71',
                    data: chartData.lru,
                    fill: false,
                    tension: 0.2
                },
                {
                    label: 'CLOCK Hit Ratio (%)',
                    borderColor: '#F1C40F',
                    data: chartData.clock,
                    fill: false,
                    tension: 0.2
                },
                {
                    label: 'OPTIMAL Hit Ratio (%)',
                    borderColor: '#E74C3C',
                    data: chartData.optimal,
                    fill: false,
                    tension: 0.2
                }
            ]
        },
        options: {
            responsive: true,
            scales: {
                y: { 
                    beginAtZero: true,
                    max: 100
                }
            }
        }
    });
}

function updateChart(data) {
    const time = new Date().toLocaleTimeString();

    chartData.labels.push(time);
    if (chartData.labels.length > 20) chartData.labels.shift();

    chartData.fifo.push((data.fifo.hitRatio * 100).toFixed(2));
    chartData.lru.push((data.lru.hitRatio * 100).toFixed(2));
    chartData.clock.push((data.clock.hitRatio * 100).toFixed(2));
    chartData.optimal.push((data.optimal.hitRatio * 100).toFixed(2));

    // Keep last 20 points
    chartData.fifo = chartData.fifo.slice(-20);
    chartData.lru = chartData.lru.slice(-20);
    chartData.clock = chartData.clock.slice(-20);
    chartData.optimal = chartData.optimal.slice(-20);

    chartRef.update();
}

// ✅ Initial chart setup
setupChart();

ws.onmessage = (msg) => {
    const data = JSON.parse(msg.data);

    // ✅ Show current page accessed
    currentPage.textContent = data.page;

    // ✅ Update algorithm cards
    algoDiv.innerHTML = `
        ${renderAlgo("FIFO", data.fifo)}
        ${renderAlgo("LRU", data.lru)}
        ${renderAlgo("CLOCK", data.clock)}
        ${renderAlgo("OPTIMAL", data.optimal)}
    `;

    // ✅ Update graph
    updateChart(data);
};

// ✅ Render each algorithm card
function renderAlgo(name, obj) {
    return `
        <div class="algo-box">
            <div class="tag">${name}</div>

            <div class="frames">
                ${obj.frames.map(f => `<span class="path">${f}</span>`).join("")}
            </div>

            <div><strong>Hits:</strong> ${obj.hits}</div>
            <div><strong>Misses / Faults:</strong> ${obj.misses}</div>
            <div><strong>Hit Ratio:</strong> ${(obj.hitRatio * 100).toFixed(2)}%</div>
        </div>
    `;
}
