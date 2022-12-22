import { AfterViewInit, Component, ElementRef, Input, OnInit, ViewChild } from '@angular/core';
import * as d3 from 'd3';

@Component({
  selector: 'app-ad-line-chart',
  templateUrl: './ad-line-chart.component.html',
  styleUrls: ['./ad-line-chart.component.scss']
})

export class AdLineChartComponent implements OnInit, AfterViewInit {
  @ViewChild('containerLineChart', { static: true }) chartContainer: ElementRef;
  @Input('lineChartData') public anomalyData: any = [];
  @Input('modelName') public modelName;
  @Input('ChartWidth') public ChartWidth: number;
  @Input('ChartHeight') public ChartHeight: number;

  svg;
  margin = 10;
  width: number;
  height: number;
  barWidth: Number = 4;
  textFont: string = '6px';
  legendTextFont: string = '5px';
  strokeColor: string = 'lightgrey';
  strokeWidth: string = '0.5';
  strokeOpacity: string = '0.7';
  legendRectWidth : string = '10px';
  legendRectHeight : string = '4px';
  chartData = [];
  pageSize: number = 30;
  pageNumber: number = 1;
  dataCopy = [];
  legendSvg;
  circleRadius : number = 1;

  legend = [{ name: 'Anomaly', color: 'red' }, { name: 'Non Anomaly', color: 'blue' }];

  constructor() {
  }

  ngOnInit(): void {
    this.dataCopy = this.anomalyData;
    this.width = this.ChartWidth - this.margin * 2;
    this.height = this.ChartHeight - this.margin * 2;

    for (var i = 0; i < this.pageSize; i++) {
      this.chartData.push(this.anomalyData[i]);
    }

    this.createLegendSvg();
    
  }

  ngAfterViewInit(): void {
    this.createSvg();
    this.drawBars(this.chartData);
  }

  private createSvg(): void {
    this.svg = d3
      .select('div#chart')
      .append('svg')
      .attr(
        'viewBox',
        `0 0 ${this.width + this.margin * 2} ${this.height + this.margin * 2}`
      )

      .append('g')
      .attr('transform', 'translate(' + this.margin + ',' + this.margin + ')');
  }

  private createLegendSvg(): void {
    this.legendSvg = d3
      .select('div#legend')
      .append('svg')
      .attr(
        'viewBox',
        `0 0 ${this.width + this.margin * 2} ${this.margin + 5}`
      )

      .append('g')
      .attr('transform', 'translate(' + this.margin + ',' + this.margin + ')');
      this.addLegend();
  }

  private drawBars(data: any[]): void {

// Create X-axis band scale
    // var xScale = d3
    //     .scaleTime()
    //     .domain(d3.extent(data[0].values, (d) => d.date))
    //     .range([0, this.width]);
        
        var xScale = d3.scaleBand().range([0, this.width])
        //.domain(d3.extent(this.anomalyData[0].values, (d) => d.date))
        .domain(data.map((d) => {
          if(d) return d.date}));
          //.domain(data.forEach((a)=>a.date))
        // Create Y-axis band scale
    var yScale = d3
        .scaleLinear()
        .domain([0, d3.max(data, (d) => {if(d) return d.Actual})])
        .range([this.height, 0]);

    // Drawing X-axis on the DOM
    this.svg
      .append('g')
      .attr("class", "x axis")
      .attr('transform', 'translate(0,' + this.height + ')')
      .call(d3.axisBottom(xScale).tickSizeOuter(0).tickSize(0).tickPadding(2).tickSizeInner(-this.height))
      .selectAll('text')
      .attr('transform', 'translate(0, 10)rotate(-90)')// rotate x axis lables
      //.attr('transform', 'translate(0, 0)rotate(-90)')// rotate x axis lables
      .style('text-anchor', 'middle')
      .style('font-size', this.textFont);

    

    // Draw the Y-axis on the DOM
    this.svg
      .append('g')
      .call(d3.axisLeft(yScale).tickSizeOuter(0).tickSize(0).tickPadding(2).tickSizeInner(-this.width))
      .selectAll('text')
      .style('font-size', this.textFont);

      this.addGridStyles();

    // var lineGen = d3.line()
    // .curve(d3.curveCardinal)
    //   .x(function (d) {
    //     if(d)
    //     return xScale(d.date);
    //   })
    //   .y(function (d) {
    //     if(d)
    //     return yScale(d.Input);
    //   });

    //   this.svg.append('svg:path')
    //   .attr('d', lineGen(data))
    //   .attr('stroke', '#427CB0')
    //   .attr('stroke-width', 1)
    //   .attr('fill', 'none');

      var lineGen = d3.line()
      .curve(d3.curveCardinal)
      .x(function (d) {
        if(d)
        return xScale(d.date);
      })
      .y(function (d) {
        if(d)
        return yScale(d.Actual);
      });

      this.svg.append('svg:path')
      .attr('d', lineGen(data))
      .attr('stroke', '#EC8338')
      .attr('stroke-width', 1)
      .attr('fill', 'none');

      var lineGen = d3.line()
      .curve(d3.curveCardinal)
      .x(function (d) {
        if(d)
        return xScale(d.date);
      })
      .y(function (d) {
        if(d)
        return yScale(d.Forecast);
      });

      this.svg.append('svg:path')
      .attr('d', lineGen(data))
      .attr('stroke', '#eebd70')
      .attr('stroke-width', 1)
      .attr('fill', 'none');

      this.svg.append("g").selectAll('circle-group')
      .data(data)
      .enter()
      .append('g')
      .style('fill', '#8B78DB')
      .style('stroke', '#8B78DB')
      .style('stroke-width', '0.5')
      .selectAll('circle')
      .data(data)
      .enter()
      .append('g')
      .attr('class', 'circle')
      .append('circle')
      .attr('class', function (d) {
        if(d)
        return d.Actual === undefined ? 'NaN' : d.Actual;
      })
      .attr('cx', (d) => {
        if(d)
        return xScale(d.date)})
      .attr('cy', (d) => {
        if(d)
        return yScale(d.Actual)})
      .attr('r', this.circleRadius)
      .style('opacity', '1');

    //this.updateBar(data, x, y);
  }

  private updateBar(data, x, y) {
    // Create and fill the bars
    this.svg
      .selectAll('bars')
      .data(data)
      .enter()
      .append('rect')
      // .attr('x', (d) => {
      //   const val = x(d.key + d.Xaxis);
      //   return val ? + val + 2 : 0
      // })
      .attr('x', (d) => x(d.key + d.Xaxis))
      .attr('y', (d) => y(d.Yaxis))
      .attr('width', x.bandwidth())
      .attr('height', (d) =>
        y(d.Yaxis) < this.height ? this.height - y(d.Yaxis) : this.height
      ) // this.height
      .attr('fill', (d) => { return (d.Xaxis > 0) ? this.legend[1].color : this.legend[0].color });

    //this.addHeaderFooter();
    this.addGridStyles();
  }


  private addLegend() {
    var legend = this.legendSvg.append("g")
      .attr("class", "legend")
      .attr("x", 0)
      .attr("y", 0)
      .attr("height", 2)
      .attr("width", 2)
      .style("border", '1px');

    legend.append("rect")
      .attr("x", 0)
      .attr("y", -10)
      .attr("width", this.legendRectWidth)
      .attr("height", this.legendRectHeight)
      .style("fill", this.legend[0].color);

    legend.append("text")
      .attr("x", 12)
      .attr("y", -6)
      .text(this.legend[0].name)
      .style('font-size', this.textFont);

    legend.append("rect")
      .attr("x", 0)
      .attr("y", -3)
      .attr("width", this.legendRectWidth)
      .attr("height", this.legendRectHeight)
      .style("fill", this.legend[1].color);


    legend.append("text")
      .attr("x", 12)
      .attr("y", 0)
      .text(this.legend[1].name)
      .style('font-size', this.textFont);

  }

  private addGridStyles() {
    this.svg.selectAll('path').attr('stroke', this.strokeColor).attr('stroke-width', this.strokeWidth).attr('opacity', this.strokeOpacity);
    this.svg.selectAll('line').attr('stroke', this.strokeColor).attr('stroke-width', this.strokeWidth).attr('opacity', this.strokeOpacity);
  }

  private changePage($event) {
    this.pageNumber = $event;
    this.chartData = this.dataCopy.slice((this.pageNumber - 1) * this.pageSize, this.pageNumber * this.pageSize);
    this.deleteElementIfAlready();
    this.createSvg();
    this.drawBars(this.chartData);
  }

  private deleteElementIfAlready() {
    let element = this.chartContainer.nativeElement;
    if (element.hasChildNodes()) {
      let eleLength = element.childNodes.length;
      do {
        const childNodes = Array.from(element.childNodes);

        Array.prototype.forEach.call(childNodes, function (childNode) {
          childNode.remove();
        });

        eleLength = element.childNodes.length;
      } while (eleLength > 0);
    }
  }

}

