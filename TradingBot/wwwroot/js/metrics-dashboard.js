// TradingBot Metrics Dashboard
class MetricsDashboard {
    constructor() {
        this.charts = {};
        this.updateInterval = null;
        this.currentPeriod = '1h';
        this.metricsData = {};
        this.init();
    }

    init() {
        this.setupEventListeners();
        this.initializeCharts();
        this.startRealTimeUpdates();
        this.loadInitialData();
    }

    setupEventListeners() {
        // Период для графиков
        document.querySelectorAll('.chart-controls .btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                document.querySelectorAll('.chart-controls .btn').forEach(b => b.classList.remove('active'));
                e.target.classList.add('active');
                this.currentPeriod = e.target.dataset.period;
                this.updateChartsPeriod();
            });
        });

        // Кнопка обновления
        document.getElementById('refreshBtn').addEventListener('click', () => {
            this.refreshData();
        });

        // Автообновление каждые 30 секунд
        this.updateInterval = setInterval(() => {
            this.updateMetrics();
        }, 30000);
    }

    initializeCharts() {
        // График сообщений
        this.charts.messages = new Chart(document.getElementById('messagesChart'), {
            type: 'line',
            data: {
                labels: [],
                datasets: [{
                    label: 'Сообщения/мин',
                    data: [],
                    borderColor: '#6366f1',
                    backgroundColor: 'rgba(99, 102, 241, 0.1)',
                    borderWidth: 3,
                    fill: true,
                    tension: 0.4,
                    pointBackgroundColor: '#6366f1',
                    pointBorderColor: '#ffffff',
                    pointBorderWidth: 2,
                    pointRadius: 4
                }]
            },
            options: this.getChartOptions('Сообщения в минуту')
        });

        // График времени ответа
        this.charts.responseTime = new Chart(document.getElementById('responseTimeChart'), {
            type: 'line',
            data: {
                labels: [],
                datasets: [{
                    label: 'Время ответа (мс)',
                    data: [],
                    borderColor: '#10b981',
                    backgroundColor: 'rgba(16, 185, 129, 0.1)',
                    borderWidth: 3,
                    fill: true,
                    tension: 0.4,
                    pointBackgroundColor: '#10b981',
                    pointBorderColor: '#ffffff',
                    pointBorderWidth: 2,
                    pointRadius: 4
                }]
            },
            options: this.getChartOptions('Время ответа системы')
        });

        // График ресурсов
        this.charts.resources = new Chart(document.getElementById('resourcesChart'), {
            type: 'line',
            data: {
                labels: [],
                datasets: [{
                    label: 'Память (MB)',
                    data: [],
                    borderColor: '#f59e0b',
                    backgroundColor: 'rgba(245, 158, 11, 0.1)',
                    borderWidth: 3,
                    fill: false,
                    tension: 0.4,
                    yAxisID: 'y'
                }, {
                    label: 'CPU (%)',
                    data: [],
                    borderColor: '#ef4444',
                    backgroundColor: 'rgba(239, 68, 68, 0.1)',
                    borderWidth: 3,
                    fill: false,
                    tension: 0.4,
                    yAxisID: 'y1'
                }]
            },
            options: this.getChartOptions('Использование ресурсов', true)
        });

        // График ошибок
        this.charts.errors = new Chart(document.getElementById('errorsChart'), {
            type: 'doughnut',
            data: {
                labels: ['Валидация', 'База данных', 'Telegram', 'Notion', 'Другие'],
                datasets: [{
                    data: [0, 0, 0, 0, 0],
                    backgroundColor: [
                        '#6366f1',
                        '#10b981',
                        '#f59e0b',
                        '#ef4444',
                        '#8b5cf6'
                    ],
                    borderWidth: 2,
                    borderColor: '#1e293b'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            color: '#f8fafc',
                            font: {
                                size: 12
                            }
                        }
                    },
                    tooltip: {
                        backgroundColor: '#1e293b',
                        titleColor: '#f8fafc',
                        bodyColor: '#cbd5e1',
                        borderColor: '#475569',
                        borderWidth: 1
                    }
                }
            }
        });
    }

    getChartOptions(title, dualY = false) {
        const options = {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    labels: {
                        color: '#f8fafc',
                        font: {
                            size: 12
                        }
                    }
                },
                tooltip: {
                    backgroundColor: '#1e293b',
                    titleColor: '#f8fafc',
                    bodyColor: '#cbd5e1',
                    borderColor: '#475569',
                    borderWidth: 1,
                    mode: 'index',
                    intersect: false
                }
            },
            scales: {
                x: {
                    type: 'time',
                    time: {
                        unit: 'minute',
                        displayFormats: {
                            minute: 'HH:mm'
                        }
                    },
                    grid: {
                        color: '#475569',
                        borderColor: '#475569'
                    },
                    ticks: {
                        color: '#cbd5e1',
                        maxTicksLimit: 8
                    }
                },
                y: {
                    beginAtZero: true,
                    grid: {
                        color: '#475569',
                        borderColor: '#475569'
                    },
                    ticks: {
                        color: '#cbd5e1'
                    }
                }
            },
            interaction: {
                mode: 'nearest',
                axis: 'x',
                intersect: false
            }
        };

        if (dualY) {
            options.scales.y1 = {
                type: 'linear',
                display: true,
                position: 'right',
                beginAtZero: true,
                max: 100,
                grid: {
                    drawOnChartArea: false
                },
                ticks: {
                    color: '#cbd5e1'
                }
            };
        }

        return options;
    }

    async loadInitialData() {
        try {
            await this.updateMetrics();
            this.updateChartsPeriod();
        } catch (error) {
            console.error('Ошибка загрузки начальных данных:', error);
            this.showError('Не удалось загрузить данные');
        }
    }

    async updateMetrics() {
        try {
            const response = await fetch('/metrics');
            if (!response.ok) throw new Error('HTTP error! status: ' + response.status);
            
            const metricsText = await response.text();
            this.parseMetrics(metricsText);
            this.updateStats();
            this.updateCharts();
            this.updateRealTimeData();
        } catch (error) {
            console.error('Ошибка обновления метрик:', error);
            this.showError('Ошибка обновления метрик');
        }
    }

    parseMetrics(metricsText) {
        const lines = metricsText.split('\n');
        this.metricsData = {};

        lines.forEach(line => {
            if (line.startsWith('#') || line.trim() === '') return;
            
            const match = line.match(/^([a-zA-Z_:][a-zA-Z0-9_:]*)\s+([0-9.]+)(?:\s+(\d+))?$/);
            if (match) {
                const metricName = match[1];
                const value = parseFloat(match[2]);
                const timestamp = match[3] ? parseInt(match[3]) : Date.now();

                if (!this.metricsData[metricName]) {
                    this.metricsData[metricName] = [];
                }
                
                this.metricsData[metricName].push({ value, timestamp });
            }
        });
    }

    updateStats() {
        // Обновляем статистику
        const messagesTotal = this.getMetricValue('tradingbot_messages_total') || 0;
        const tradesTotal = this.getMetricValue('tradingbot_trades_total') || 0;
        const errorsTotal = this.getMetricValue('tradingbot_errors_total') || 0;
        const activeUsers = this.getMetricValue('tradingbot_active_users') || 0;
        const memoryUsage = this.getMetricValue('tradingbot_memory_usage_bytes') || 0;
        const dbSize = this.getMetricValue('tradingbot_database_size_mb') || 0;

        document.getElementById('messages-count').textContent = messagesTotal.toLocaleString();
        document.getElementById('trades-count').textContent = tradesTotal.toLocaleString();
        document.getElementById('errors-count').textContent = errorsTotal.toLocaleString();
        document.getElementById('active-users').textContent = activeUsers.toLocaleString();
        document.getElementById('memory-usage').textContent = (memoryUsage / 1024 / 1024).toFixed(1);
        document.getElementById('db-size').textContent = dbSize.toFixed(1);
    }

    updateCharts() {
        this.updateMessagesChart();
        this.updateResponseTimeChart();
        this.updateResourcesChart();
        this.updateErrorsChart();
    }

    updateMessagesChart() {
        const chart = this.charts.messages;
        const data = this.generateTimeSeriesData('tradingbot_messages_total', this.currentPeriod);
        
        chart.data.labels = data.labels;
        chart.data.datasets[0].data = data.values;
        chart.update('none');
    }

    updateResponseTimeChart() {
        const chart = this.charts.responseTime;
        const data = this.generateTimeSeriesData('tradingbot_request_duration_seconds', this.currentPeriod);
        
        chart.data.labels = data.labels;
        chart.data.datasets[0].data = data.values.map(v => v * 1000); // конвертируем в мс
        chart.update('none');
    }

    updateResourcesChart() {
        const chart = this.charts.resources;
        const memoryData = this.generateTimeSeriesData('tradingbot_memory_usage_bytes', this.currentPeriod);
        const cpuData = this.generateTimeSeriesData('tradingbot_cpu_usage_percentage', this.currentPeriod);
        
        chart.data.labels = memoryData.labels;
        chart.data.datasets[0].data = memoryData.values.map(v => v / 1024 / 1024); // конвертируем в MB
        chart.data.datasets[1].data = cpuData.values;
        chart.update('none');
    }

    updateErrorsChart() {
        const chart = this.charts.errors;
        const errorTypes = ['validation', 'database', 'telegram', 'notion', 'other'];
        
        const data = errorTypes.map(type => {
            return this.getMetricValue(`tradingbot_errors_total{type="${type}"}`) || 0;
        });
        
        chart.data.datasets[0].data = data;
        chart.update('none');
    }

    generateTimeSeriesData(metricName, period) {
        const now = Date.now();
        let timeRange;
        
        switch (period) {
            case '1h': timeRange = 60 * 60 * 1000; break;
            case '6h': timeRange = 6 * 60 * 60 * 1000; break;
            case '24h': timeRange = 24 * 60 * 60 * 1000; break;
            default: timeRange = 60 * 60 * 1000;
        }

        const startTime = now - timeRange;
        const interval = timeRange / 20; // 20 точек данных
        
        const labels = [];
        const values = [];
        
        for (let i = 0; i < 20; i++) {
            const time = startTime + i * interval;
            labels.push(new Date(time));
            
            // Имитируем данные для демонстрации
            const baseValue = this.getMetricValue(metricName) || 0;
            const randomFactor = 0.8 + Math.random() * 0.4; // ±20%
            values.push(baseValue * randomFactor);
        }
        
        return { labels, values };
    }

    getMetricValue(metricName) {
        if (this.metricsData[metricName] && this.metricsData[metricName].length > 0) {
            return this.metricsData[metricName][this.metricsData[metricName].length - 1].value;
        }
        return 0;
    }

    updateChartsPeriod() {
        // Обновляем все графики с новым периодом
        this.updateCharts();
    }

    updateRealTimeData() {
        const container = document.getElementById('realTimeData');
        const now = new Date();
        
        const html = `
            <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 1rem;">
                <div style="background: rgba(99, 102, 241, 0.1); padding: 1rem; border-radius: 8px; border: 1px solid rgba(99, 102, 241, 0.3);">
                    <div style="font-size: 0.9rem; color: #cbd5e1; margin-bottom: 0.5rem;">Последнее обновление</div>
                    <div style="font-weight: 600; color: #f8fafc;">${now.toLocaleTimeString()}</div>
                </div>
                <div style="background: rgba(16, 185, 129, 0.1); padding: 1rem; border-radius: 8px; border: 1px solid rgba(16, 185, 129, 0.3);">
                    <div style="font-size: 0.9rem; color: #cbd5e1; margin-bottom: 0.5rem;">Статус системы</div>
                    <div style="font-weight: 600; color: #f8fafc;">🟢 Активна</div>
                </div>
                <div style="background: rgba(245, 158, 11, 0.1); padding: 1rem; border-radius: 8px; border: 1px solid rgba(245, 158, 11, 0.3);">
                    <div style="font-size: 0.9rem; color: #cbd5e1; margin-bottom: 0.5rem;">Время работы</div>
                    <div style="font-weight: 600; color: #f8fafc;">${this.getUptime()}</div>
                </div>
                <div style="background: rgba(239, 68, 68, 0.1); padding: 1rem; border-radius: 8px; border: 1px solid rgba(239, 68, 68, 0.3);">
                    <div style="font-size: 0.9rem; color: #cbd5e1; margin-bottom: 0.5rem;">Версия</div>
                    <div style="font-weight: 600; color: #f8fafc;">v2.0.0</div>
                </div>
            </div>
        `;
        
        container.innerHTML = html;
    }

    getUptime() {
        // Имитируем время работы
        const startTime = new Date(Date.now() - 2 * 60 * 60 * 1000); // 2 часа назад
        const uptime = Date.now() - startTime.getTime();
        const hours = Math.floor(uptime / (1000 * 60 * 60));
        const minutes = Math.floor((uptime % (1000 * 60 * 60)) / (1000 * 60));
        return `${hours}ч ${minutes}м`;
    }

    async refreshData() {
        const btn = document.getElementById('refreshBtn');
        const icon = btn.querySelector('i');
        
        // Анимация загрузки
        icon.style.transform = 'rotate(360deg)';
        btn.disabled = true;
        
        try {
            await this.updateMetrics();
            this.showSuccess('Данные обновлены');
        } catch (error) {
            this.showError('Ошибка обновления данных');
        } finally {
            setTimeout(() => {
                icon.style.transform = 'rotate(0deg)';
                btn.disabled = false;
            }, 1000);
        }
    }

    startRealTimeUpdates() {
        // Обновляем каждые 5 секунд для демонстрации
        setInterval(() => {
            this.updateRealTimeData();
        }, 5000);
    }

    showSuccess(message) {
        this.showNotification(message, 'success');
    }

    showError(message) {
        this.showNotification(message, 'error');
    }

    showNotification(message, type) {
        const notification = document.createElement('div');
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            padding: 1rem 1.5rem;
            border-radius: 8px;
            color: white;
            font-weight: 500;
            z-index: 1000;
            transform: translateX(400px);
            transition: transform 0.3s ease;
            background: ${type === 'success' ? '#10b981' : '#ef4444'};
        `;
        notification.textContent = message;
        
        document.body.appendChild(notification);
        
        // Анимация появления
        setTimeout(() => {
            notification.style.transform = 'translateX(0)';
        }, 100);
        
        // Автоматическое скрытие
        setTimeout(() => {
            notification.style.transform = 'translateX(400px)';
            setTimeout(() => {
                document.body.removeChild(notification);
            }, 300);
        }, 3000);
    }
}

// Инициализация дашборда при загрузке страницы
document.addEventListener('DOMContentLoaded', () => {
    new MetricsDashboard();
});

// Добавляем глобальные функции для отладки
window.MetricsDashboard = MetricsDashboard;

