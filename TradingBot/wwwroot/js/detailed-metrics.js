// TradingBot Detailed Metrics
class DetailedMetrics {
    constructor() {
        this.charts = {};
        this.metricsData = {};
        this.init();
    }

    init() {
        this.setupEventListeners();
        this.initializeCharts();
        this.loadMetrics();
        this.startAutoRefresh();
    }

    setupEventListeners() {
        // Кнопка копирования метрик
        document.getElementById('copyBtn').addEventListener('click', () => {
            this.copyMetricsToClipboard();
        });
    }

    initializeCharts() {
        // График тренда производительности
        this.charts.performanceTrend = new Chart(document.getElementById('performanceTrendChart'), {
            type: 'line',
            data: {
                labels: [],
                datasets: [{
                    label: 'Производительность (%)',
                    data: [],
                    borderColor: '#6366f1',
                    backgroundColor: 'rgba(99, 102, 241, 0.1)',
                    borderWidth: 3,
                    fill: true,
                    tension: 0.4
                }]
            },
            options: this.getChartOptions()
        });

        // График распределения ошибок
        this.charts.errorDistribution = new Chart(document.getElementById('errorDistributionChart'), {
            type: 'bar',
            data: {
                labels: ['Валидация', 'База данных', 'Telegram', 'Notion', 'Другие'],
                datasets: [{
                    label: 'Количество ошибок',
                    data: [0, 0, 0, 0, 0],
                    backgroundColor: [
                        '#6366f1',
                        '#10b981',
                        '#f59e0b',
                        '#ef4444',
                        '#8b5cf6'
                    ],
                    borderWidth: 0
                }]
            },
            options: this.getChartOptions()
        });

        // График использования ресурсов
        this.charts.resourceUsage = new Chart(document.getElementById('resourceUsageChart'), {
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
                    yAxisID: 'y'
                }, {
                    label: 'CPU (%)',
                    data: [],
                    borderColor: '#ef4444',
                    backgroundColor: 'rgba(239, 68, 68, 0.1)',
                    borderWidth: 3,
                    fill: false,
                    yAxisID: 'y1'
                }]
            },
            options: this.getChartOptions(true)
        });

        // График активности пользователей
        this.charts.userActivity = new Chart(document.getElementById('userActivityChart'), {
            type: 'line',
            data: {
                labels: [],
                datasets: [{
                    label: 'Активные пользователи',
                    data: [],
                    borderColor: '#10b981',
                    backgroundColor: 'rgba(16, 185, 129, 0.1)',
                    borderWidth: 3,
                    fill: true,
                    tension: 0.4
                }]
            },
            options: this.getChartOptions()
        });
    }

    getChartOptions(dualY = false) {
        const options = {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    labels: {
                        color: '#f8fafc',
                        font: { size: 11 }
                    }
                },
                tooltip: {
                    backgroundColor: '#1e293b',
                    titleColor: '#f8fafc',
                    bodyColor: '#cbd5e1',
                    borderColor: '#475569',
                    borderWidth: 1
                }
            },
            scales: {
                x: {
                    grid: {
                        color: '#475569',
                        borderColor: '#475569'
                    },
                    ticks: {
                        color: '#cbd5e1',
                        maxTicksLimit: 6
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
            }
        };

        if (dualY) {
            options.scales.y1 = {
                type: 'linear',
                display: true,
                position: 'right',
                beginAtZero: true,
                max: 100,
                grid: { drawOnChartArea: false },
                ticks: { color: '#cbd5e1' }
            };
        }

        return options;
    }

    async loadMetrics() {
        try {
            const response = await fetch('/metrics');
            if (!response.ok) throw new Error('HTTP error! status: ' + response.status);
            
            const metricsText = await response.text();
            this.parseMetrics(metricsText);
            this.updateMetrics();
            this.updateCharts();
            this.updateRawMetrics(metricsText);
        } catch (error) {
            console.error('Ошибка загрузки метрик:', error);
            this.showError('Не удалось загрузить метрики');
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

    updateMetrics() {
        // Общая производительность (имитация)
        const performanceScore = this.calculatePerformanceScore();
        document.getElementById('performance-score').textContent = `${performanceScore}%`;

        // Среднее время ответа
        const avgResponseTime = this.calculateAverageResponseTime();
        document.getElementById('avg-response-time').textContent = `${avgResponseTime}ms`;

        // Эффективность БД
        const dbEfficiency = this.calculateDatabaseEfficiency();
        document.getElementById('db-efficiency').textContent = `${dbEfficiency}%`;

        // Стабильность API
        const apiStability = this.calculateApiStability();
        document.getElementById('api-stability').textContent = `${apiStability}%`;
    }

    calculatePerformanceScore() {
        // Имитируем расчет производительности на основе метрик
        const messagesTotal = this.getMetricValue('tradingbot_messages_total') || 0;
        const errorsTotal = this.getMetricValue('tradingbot_errors_total') || 0;
        const memoryUsage = this.getMetricValue('tradingbot_memory_usage_bytes') || 0;
        
        let score = 85; // Базовый балл
        
        // Учитываем количество сообщений (больше = лучше)
        if (messagesTotal > 100) score += 10;
        else if (messagesTotal > 50) score += 5;
        
        // Учитываем ошибки (меньше = лучше)
        if (errorsTotal < 5) score += 5;
        else if (errorsTotal > 20) score -= 10;
        
        // Учитываем использование памяти
        const memoryMB = memoryUsage / 1024 / 1024;
        if (memoryMB < 100) score += 5;
        else if (memoryMB > 500) score -= 10;
        
        return Math.max(0, Math.min(100, score));
    }

    calculateAverageResponseTime() {
        const responseTime = this.getMetricValue('tradingbot_request_duration_seconds') || 0;
        return Math.round(responseTime * 1000); // конвертируем в мс
    }

    calculateDatabaseEfficiency() {
        // Имитируем эффективность БД
        const dbSize = this.getMetricValue('tradingbot_database_size_mb') || 0;
        let efficiency = 90; // Базовый балл
        
        if (dbSize > 100) efficiency -= 10;
        if (dbSize > 500) efficiency -= 20;
        
        return Math.max(0, Math.min(100, efficiency));
    }

    calculateApiStability() {
        // Имитируем стабильность API
        const errorsTotal = this.getMetricValue('tradingbot_errors_total') || 0;
        const messagesTotal = this.getMetricValue('tradingbot_messages_total') || 1;
        
        const errorRate = errorsTotal / messagesTotal;
        let stability = 95; // Базовый балл
        
        if (errorRate > 0.1) stability -= 20;
        if (errorRate > 0.05) stability -= 10;
        
        return Math.max(0, Math.min(100, stability));
    }

    updateCharts() {
        this.updatePerformanceTrendChart();
        this.updateErrorDistributionChart();
        this.updateResourceUsageChart();
        this.updateUserActivityChart();
    }

    updatePerformanceTrendChart() {
        const chart = this.charts.performanceTrend;
        const data = this.generatePerformanceData();
        
        chart.data.labels = data.labels;
        chart.data.datasets[0].data = data.values;
        chart.update('none');
    }

    updateErrorDistributionChart() {
        const chart = this.charts.errorDistribution;
        const errorTypes = ['validation', 'database', 'telegram', 'notion', 'other'];
        
        const data = errorTypes.map(type => {
            return this.getMetricValue(`tradingbot_errors_total{type="${type}"}`) || 0;
        });
        
        chart.data.datasets[0].data = data;
        chart.update('none');
    }

    updateResourceUsageChart() {
        const chart = this.charts.resourceUsage;
        const data = this.generateResourceData();
        
        chart.data.labels = data.labels;
        chart.data.datasets[0].data = data.memory;
        chart.data.datasets[1].data = data.cpu;
        chart.update('none');
    }

    updateUserActivityChart() {
        const chart = this.charts.userActivity;
        const data = this.generateUserActivityData();
        
        chart.data.labels = data.labels;
        chart.data.datasets[0].data = data.values;
        chart.update('none');
    }

    generatePerformanceData() {
        const labels = [];
        const values = [];
        const now = Date.now();
        
        for (let i = 23; i >= 0; i--) {
            const time = new Date(now - i * 60 * 60 * 1000);
            labels.push(time.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' }));
            
            // Имитируем данные производительности
            const baseScore = this.calculatePerformanceScore();
            const variation = (Math.random() - 0.5) * 20;
            values.push(Math.max(0, Math.min(100, baseScore + variation)));
        }
        
        return { labels, values };
    }

    generateResourceData() {
        const labels = [];
        const memory = [];
        const cpu = [];
        const now = Date.now();
        
        for (let i = 23; i >= 0; i--) {
            const time = new Date(now - i * 60 * 60 * 1000);
            labels.push(time.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' }));
            
            // Имитируем данные ресурсов
            const baseMemory = (this.getMetricValue('tradingbot_memory_usage_bytes') || 100 * 1024 * 1024) / 1024 / 1024;
            const baseCpu = this.getMetricValue('tradingbot_cpu_usage_percentage') || 15;
            
            memory.push(baseMemory + (Math.random() - 0.5) * 20);
            cpu.push(baseCpu + (Math.random() - 0.5) * 10);
        }
        
        return { labels, memory, cpu };
    }

    generateUserActivityData() {
        const labels = [];
        const values = [];
        const now = Date.now();
        
        for (let i = 23; i >= 0; i--) {
            const time = new Date(now - i * 60 * 60 * 1000);
            labels.push(time.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' }));
            
            // Имитируем активность пользователей
            const baseUsers = this.getMetricValue('tradingbot_active_users') || 5;
            const variation = (Math.random() - 0.5) * 3;
            values.push(Math.max(0, baseUsers + variation));
        }
        
        return { labels, values };
    }

    getMetricValue(metricName) {
        if (this.metricsData[metricName] && this.metricsData[metricName].length > 0) {
            return this.metricsData[metricName][this.metricsData[metricName].length - 1].value;
        }
        return 0;
    }

    updateRawMetrics(metricsText) {
        document.getElementById('rawMetrics').value = metricsText;
    }

    async copyMetricsToClipboard() {
        try {
            const textarea = document.getElementById('rawMetrics');
            await navigator.clipboard.writeText(textarea.value);
            this.showSuccess('Метрики скопированы в буфер обмена');
        } catch (error) {
            console.error('Ошибка копирования:', error);
            this.showError('Не удалось скопировать метрики');
        }
    }

    startAutoRefresh() {
        // Обновляем каждые 30 секунд
        setInterval(() => {
            this.loadMetrics();
        }, 30000);
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

// Инициализация при загрузке страницы
document.addEventListener('DOMContentLoaded', () => {
    new DetailedMetrics();
});

