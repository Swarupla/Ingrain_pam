import { Component, OnInit, AfterViewInit, ViewChild, ElementRef, Input, OnChanges, EventEmitter, Output } from '@angular/core';
import * as d3 from 'd3';

@Component({
  selector: 'app-horizontal-chart',
  templateUrl: './horizontal-chart.component.html',
  styleUrls: ['./horizontal-chart.component.scss']
})
export class HorizontalChartComponent implements OnInit, AfterViewInit, OnChanges {

  @ViewChild('containerBarChart', { static: true }) containerBarChart: ElementRef;
  htmlElement: HTMLElement;
  host;
  svg;
  _horizontalBarchartWidth;
  @Input() horizontalBarchartHeight: number;
  @Input() horizontalBarchartWidth: number;
  @Input() data: Array<any>;
  @Input() assetUsage;
  @Output() barClicked = new EventEmitter();
  margin: any = { top: 0, bottom: 20, left: 60, right: 20 };

  constructor() { }

  ngOnInit() { }

  ngOnChanges(changes) {
    this.createChart();
  }

  ngAfterViewInit() {
    this.data = this.data = this.data.sort((a, b) => (a.prcntvalue > b.prcntvalue) ? 1 : ((b.prcntvalue > a.prcntvalue) ? -1 : 0));
    this.createChart();
  }

  createChart() {
    const _this = this;
    const oldWidth = this.horizontalBarchartWidth;
    const oldHeight = this.horizontalBarchartHeight;

    const element = this.containerBarChart.nativeElement;
    element.innerText = '';

    const margin = {
      top: 20,
      right: 25,
      bottom: 20,
      left: 60
    };

    const width = oldWidth - margin.left - margin.right,
      height = oldHeight - margin.top - margin.bottom;

    const div = d3.select(element).append('div')
      .attr('class', 'tooltip')
      .style('opacity', 0);
    const svg = d3.select(element).append('svg')
      .attr('width', width + margin.left + margin.right)
      .attr('height', height + margin.top + margin.bottom)
      .append('g')
      .attr('transform', 'translate(' + (margin.left - 10) + ',0)');

      const maxValue = d3.max(this.data, function (d) {
        return d.prcntvalue;
      });

    const x = d3.scaleLinear()
      .range([0, width])
      .domain([0, d3.max(this.data, function (d) {
        return d.prcntvalue;
      })])
      .nice();

    const y = d3.scaleBand()
      .rangeRound([height, 0])
      .padding(0.5)
      .domain(this.data.map(function (d) {
        return d.featurename;
      }));

    // gridlines in x axis function
    const xAxisGrid = d3.axisBottom(x).tickSize(-height).tickFormat('');

    // add the X gridlines
    if ( maxValue === 0) {
      svg.append('g')
      .attr('class', 'x x-axis-grid')
      .attr('transform', 'translate(0,' + height + ')')
      .call(xAxisGrid)
      .selectAll('.tick')
      .attr('transform', 'translate(0, 0)')
      .attr('stroke', 'lightblue')
      .attr('opacity', '0.2');
    } else {
      svg.append('g')
      .attr('class', 'x x-axis-grid')
      .attr('transform', 'translate(0,' + height + ')')
      .call(xAxisGrid)
      .selectAll('.tick')
      .attr('stroke', 'lightblue')
      .attr('opacity', '0.2');
    }


    let sequentialScale = d3.scaleSequential()
      .domain([0, d3.max(this.data, function (d) {
        return d.prcntvalue;
      })])
      .interpolator(d3.interpolateRainbow);

    // make y axis to show bar names
    const yAxis = d3.axisLeft()
      .scale(y)
      // no tick marks
      .tickSize(0);

    const gy = svg.append('g')
      .attr('class', 'y-axis-text')
      .attr('font-size', '12px')
      .attr('color', 'grey')
      .call(yAxis);

    gy.select('.domain')
      .attr('stroke-width', 0);

    const bars = svg.selectAll('.bar')
      .data(this.data)
      .enter()
      .append('g');

    const rx = y.bandwidth() / 2;
    const ry = y.bandwidth() / 2;

    // append rects
    bars.append('path')
      .attr('class', 'bar')
      /* .attr('y', function (d) {
        return y(d.featurename);
      })
      .attr('height', y.bandwidth())
      .attr('x', 0)
      .attr('width', function (d) {
        return x(d.prcntvalue);
      }) */
      .attr('d', function (d) {
        if (d.prcntvalue == 0) {
          return `
              M0,${y(d.featurename)}
              h0
              a${rx},${ry} 0 0 1 ${rx},${ry}
              v0
              a${rx},${ry} 0 0 1 ${-rx},${ry}
              h0Z
            `;
        } else if (d.prcntvalue > 0) {
          if ((x(d.prcntvalue) - rx) < 0) {
            return `
            M0,${y(d.featurename)}
            h0
            a${rx},${ry} 0 0 1 ${rx},${ry}
            v0
            a${rx},${ry} 0 0 1 ${-rx},${ry}
            h0Z
            `;
          } else {
            return `
              M0,${y(d.featurename)}
              h${x(d.prcntvalue) - rx}
              a${rx},${ry} 0 0 1 ${rx},${ry}
              v0
              a${rx},${ry} 0 0 1 ${-rx},${ry}
              h${-(x(d.prcntvalue) - rx)}Z
            `;
          }
        } else {
          return `
              M0,${y(d.featurename)}
              h${-(x(d.prcntvalue) - rx)}
              a${rx},${ry} 1 0 0 ${-rx},${ry}
              v0
              a${rx},${ry} 1 0 0 ${rx},${ry}
              h${(x(d.prcntvalue) - rx)}Z
            `;
        }
      })
      .on('click', function (d: any) {
        _this.barClicked.emit(d);
      })
      .style('cursor', 'pointer')
      .attr('fill', function (d, i) {
        return sequentialScale(i * 10);
      });

    const textUnit = (_this.assetUsage) ? '' : '%'
    // add a value label to the right of each bar
    bars.append('text')
      .attr('class', 'label')
      // y position of the label is halfway down the bar
      .attr('y', function (d) {
        return y(d.featurename) + y.bandwidth() / 2 + 4;
      })
      // x position is 3 pixels to the right of the bar
      .attr('x', function (d) {
        if ( maxValue === 0) {
          return 0;
        } else {
        return x(d.prcntvalue) + 3;
        }
      })
      .attr('font-size', 12)
      .attr('font-family', 'sans-serif')
      .text(function (d) {
        return d.prcntvalue + textUnit;
      });

    // add the x Axis
    svg.append('g')
      .attr('transform', 'translate(0,' + height + ')')
      .classed('xAxis', true)
      .attr('color', 'grey')
      .call(d3.axisBottom(x)
        /* .tickValues(function (d) {
          return d.prcntvalue;
        }) */
        .tickFormat(d => d + textUnit));

    svg.append('text')
      .attr('transform',
        'translate(' + (width / 2) + ' ,' +
        (height + this.margin.top + 35) + ')')
      .style('text-anchor', 'middle')
      .style('font-size', '12px')
      .style('font-weight', '500')
      .text((_this.assetUsage) ? 'Count' : 'Percentage or Value Scale');


    const wrapText = _this.wrapWordEllipse;
    svg.selectAll('.y-axis-text text')
      .call(wrapText, 30)
      .on('mouseover', function (d) {
        div.transition()
          .duration(400)
          .style('opacity', .9);
        div.html(d)
          .style('left', _this.setLeft(d3.event))
          // .style('background-color', 'transparent')
          .style('top', _this.setTop(d3.event));
      })
      .on('mouseout', function (d) {
        div.transition()
          .duration(500)
          .style('opacity', 0);
      });

    d3.selectAll('g.xAxis').select('path').attr('stroke', 'lightgrey').attr('opacity', '0.7');
    d3.selectAll('g.y-Axis').select('path').attr('stroke', 'lightgrey').attr('opacity', '0.7');
    d3.selectAll('g.x-Axis').selectAll('.tick line').remove();
    d3.selectAll('g.xAxis').selectAll('.tick line').remove();
    d3.selectAll('g.y-Axis').selectAll('.tick line').remove();
    d3.selectAll('g.x-axis-grid').select('path').remove();
    d3.selectAll('g.y-axis-grid').select('path').remove();

    /* const y = d3.scaleBand()
      .range([height, 0])
      .padding(0.1);

    const x = d3.scaleLinear()
      .range([0, width]);

    const element = this.containerBarChart.nativeElement;

    const svgEle = d3.select(element).append('svg');
    const div = d3.select(element).append('div')
    .attr('class', 'tooltip')
    .style('opacity', 0);
    const svg = svgEle
      .attr('width', width + this.margin.left + this.margin.left)
      .attr('height', height + 60)
      .append('g')
      .attr('transform',
        'translate(' + this.margin.left + ',' + this.margin.top + ')');

    this.data.forEach(function (d) {
      d.prcntvalue = +d.prcntvalue;
    });

    let maximumPercentage =  d3.max(this.data, function (d) { return d.prcntvalue; });
    const dif = 100 - maximumPercentage;
    if ( dif !== 0) {
    maximumPercentage = maximumPercentage + 5;
    }

    x.domain([0, maximumPercentage]);
    y.domain(this.data.map(function (d) { return d.featurename; }));


    const yAxis = d3.axisLeft()
      .scale(y)
      // no tick marks
      .tickSize(0);

    const gy = svg.append('g')
      .classed('yAxis', true)
      .attr('transform', 'translate(-8,-30)')
      .call(yAxis);

    gy.select('.domain')
      .attr('stroke-width', 0);

    // append the rectangles for the bar chart
    svg.selectAll('.bar')
      .data(this.data)
      .enter().append('rect')
      .attr('x', function (d) { return x(Math.min(0, d.prcntvalue)); })
      .attr('width', function (d) { return Math.abs(x(d.prcntvalue) - x(0)); })
      .attr('y', function (d) { return y(d.featurename); })
      // .attr('height', 14)
      .attr('height', 28)
      .attr('fill', function (d) {
        return d['color'];
      });


     svg.append('text')
      .attr('transform',
          'translate(' + (width / 2 + 75) + ' ,' +
          (height + this.margin.top + 35) + ')')
      .style('text-anchor', 'middle')
      .text('Percentage or Value Scale');

    // add the x Axis
    svg.append('g')
      .attr('transform', 'translate(0,' + height + ')')
      .classed('xAxis', true)
      .call(d3.axisBottom(x)
        .tickValues([0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100])
        .tickFormat(d => d + '%')
        .tickSizeOuter(0));



    const textLabel = svg.append('g')
      .attr('fill', 'black')
      .attr('text-anchor', 'start')
      .attr('font-family', 'sans-serif')
      .attr('font-size', 12)
      .attr('transform', 'translate(0,0)');

    textLabel.selectAll('text')
      .data(this.data)
      .join('text')
      .attr('x', d => x(d.prcntvalue) + 3)
      .attr('y', (d, i) => y(d.featurename) + 15 / 2 + 4)
      .attr('dy', '1.5em')
      .text(d => d.prcntvalue + '%'); */



    // To show tooltip with values
    // svg.selectAll('rect')
    //   .on('mouseover', function (d) {
    //     div.transition()
    //       .duration(400)
    //       .style('opacity', .9);
    //     div.html('<div class= text-align><b>Perdiction : </b>' + d.featurename + '</div><div class=text-align><b>Weight : </b>' +
    //       d.prcntvalue + '</div>')
    //       .style('left', 0)
    //       .style('top', 0);
    //   })
    //   .on('mouseout', function (d) {
    //     div.transition()
    //       .duration(500)
    //       .style('opacity', 0);
    //   });

  }

  wrapWordEllipse(label, width) {
    label.each(function () {
      // const self = d3.select(this);
      // let textLength = label.length;
      // let text = self.text();
      let text = d3.select(this);
      text = text._groups[0][0];
      if (text.textContent.length > 5) {
        text.textContent = text.textContent.substring(0, 5) + '..';
      }
    });
  }

  setLeft(event) {
    let left;
    left = (event.layerX + 10) + 'px';
    return left;
  }
  setTop(event) {
    let top;
    top = (event.layerY + 20) + 'px';
    return top;
  }
}
