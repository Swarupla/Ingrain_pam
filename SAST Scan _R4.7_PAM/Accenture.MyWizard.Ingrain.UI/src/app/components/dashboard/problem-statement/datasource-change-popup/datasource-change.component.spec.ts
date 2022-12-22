import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { DatasourceChangeComponent } from './datasource-change.component';

describe('DatasourceChangeComponent', () => {
  let component: DatasourceChangeComponent;
  let fixture: ComponentFixture<DatasourceChangeComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ DatasourceChangeComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DatasourceChangeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
