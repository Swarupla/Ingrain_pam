import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { CurvedVerticalBarGraphComponent } from './curved-vertical-bar-graph.component';

describe('CurvedVerticalBarGraphComponent', () => {
  let component: CurvedVerticalBarGraphComponent;
  let fixture: ComponentFixture<CurvedVerticalBarGraphComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ CurvedVerticalBarGraphComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CurvedVerticalBarGraphComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
