import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ChooseCascadeModelsComponent } from './choose-cascade-models.component';

describe('ChooseCascadeModelsComponent', () => {
  let component: ChooseCascadeModelsComponent;
  let fixture: ComponentFixture<ChooseCascadeModelsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ChooseCascadeModelsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ChooseCascadeModelsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
